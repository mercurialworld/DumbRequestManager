using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BeatSaverSharp.Models;
using DumbRequestManager.API.Routes;
using DumbRequestManager.Classes;
using DumbRequestManager.Managers;
using IPA.Utilities;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Zenject;
using PluginConfig = DumbRequestManager.Configuration.PluginConfig;

namespace DumbRequestManager.Services;

[JsonObject(MemberSerialization.OptIn)]
internal class VersionOutput
{
    [JsonProperty] internal static string GameVersion => UnityGame.GameVersion.ToString();
    [JsonProperty] internal static string ModVersion => Plugin.PluginVersion.ToString();
}

[UsedImplicitly]
internal class HttpApi : IInitializable
{
    private static PluginConfig Config => PluginConfig.Instance;

    private static HttpListener? _httpListener;

    // ReSharper disable once InconsistentNaming
    private static readonly Regex WIPBotRegex = new("^[0-9A-F]{6}$");
    private static readonly Regex HawkWipRegex = new("^[0-9a-fA-F]{5}$");
    
    public void Initialize()
    {
        if (_httpListener != null)
        {
            return;
        }
        
        Plugin.Log.Info("Initializing HttpApi...");
        _httpListener = new HttpListener
        {
            Prefixes = { $"http://{Config.HttpAddress}:{Config.HttpPort}/" }
        };

        switch (Config.HttpAddress)
        {
            case "localhost":
                _httpListener.Prefixes.Add($"http://127.0.0.1:{Config.HttpPort}/");
                break;
            case "127.0.0.1":
                _httpListener.Prefixes.Add($"http://localhost:{Config.HttpPort}/");
                break;
        }

        try
        {
            _httpListener.Start();
        }
        catch (System.Net.Sockets.SocketException)
        {
            Plugin.Log.Warn($"Unable to start HTTP server on {Config.HttpAddress}:{Config.HttpPort}. More than likely, this port is already being used on this address.");
            return;
        }

        Plugin.Log.Info("HTTP server started");
        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    HttpListenerContext context = await _httpListener.GetContextAsync();
                    await HandleContext(context);
                }
                catch (Exception e)
                {
                    Plugin.Log.Error(e);
                }
            }
            // ReSharper disable once FunctionNeverReturns
            // this is an intentional infinite loop
        });
    }

    private static async Task<KeyValuePair<int, byte[]>> HandleQueryContext(string[] path)
    {
        int code = 400;
        byte[] response = Encoding.Default.GetBytes("{\"message\": \"Invalid request\"}");
        
        if (!int.TryParse(path.Last().Replace("/", string.Empty).ToLower(), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out int _))
        {
            goto finalResponse;
        }
        
        byte[]? queryResponse = path[2][..^1].ToLower() == "nocache"
            ? await QuerySkipCache(path.Last().Replace("/", string.Empty))
            : await Query(path.Last().Replace("/", string.Empty));
                
        // ReSharper disable once InvertIf
        if (queryResponse != null)
        {
            code = 200;
            response = queryResponse;
        }
        
        finalResponse:
            return new KeyValuePair<int, byte[]>(code, response);
    }
    
    private static readonly HttpClient HeadersFetchClient = new()
    {
        MaxResponseContentBufferSize = 1024
    };
    private static async Task<KeyValuePair<int, byte[]>> HandleAddLinkContext(string url, NameValueCollection urlQuery)
    {
        if (!HeadersFetchClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            HeadersFetchClient.DefaultRequestHeaders.Add("User-Agent",
                $"DumbRequestManager/{Plugin.PluginVersion} (https://github.com/TheBlackParrot/DumbRequestManager)");
        }

        int code = 400;
        byte[] response = Encoding.Default.GetBytes("{\"message\": \"Invalid request\"}");

        if (int.TryParse(url, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int _))
        {
            // https://wipbot.com codes are ^[0-9A-F]{6}$
            if (WIPBotRegex.IsMatch(url)) 
            {
                url = $"https://wipbot.com/wips/{url}.zip";
            } 
            // https://wip.hawk.quest codes are ^[0-9a-fA-F]{5}$
            else if (HawkWipRegex.IsMatch(url))
            {
                url = $"https://wip.hawk.quest/upload/{url}.zip";
            }
            else
            {
                response = Encoding.Default.GetBytes("{\"message\": \"Invalid WIP code\"}");
                goto finalResponse;
            }
        }

        bool isValidURL = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) 
                      && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        if (!isValidURL)
        {
            goto finalResponse;
        }

        string[] domainParts = uriResult.DnsSafeHost.Split(".");
        string domain = $"{domainParts[^2]}.{domainParts[^1]}";
        if (!Config.WhitelistedWipDomains.Contains(domain))
        {
            response = Encoding.Default.GetBytes("{\"message\": \"WIP URL host domain (" + domain + ") is not whitelisted, please check your configuration file\"}");
            goto finalResponse;
        }
        
        // some file hosts navigate you to a page *to* initiate the download, rather than initiating it immediately. lazy way of handling that
        url = url.Replace("?dl=0", "?dl=1");
        url = url.Replace("&dl=0", "&dl=1");

        HttpResponseMessage headResponseMessage;
        try
        {
            headResponseMessage = await HeadersFetchClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
        }
        catch (HttpRequestException)
        {
            try
            {
                Plugin.Log.Warn($"{domain} does not support HEAD requests, using a GET request instead");
                headResponseMessage = await HeadersFetchClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
            }
            catch (HttpRequestException)
            {
                Plugin.Log.Warn($"{domain} appears to be unreachable");
                response = Encoding.Default.GetBytes("{\"message\": \"WIP URL host domain (" + domain + ") does not appear to be reachable\"}");
                goto finalResponse;
            }
        }

        Plugin.DebugMessage($"WIP is {headResponseMessage.Content.Headers.ContentLength} bytes in size (according to headers)");
        if (headResponseMessage.Content.Headers.ContentLength <= 0) 
        {
            // Danielduel's wipbot has a hard-coded limit of 64MB
            // https://github.com/Danielduel/wipbot-website/blob/0b4e511c246ff934a1b2b1fde12c9b77525a0457/process/uploadWip.ts#L25
            response = Encoding.Default.GetBytes("{\"message\": \"WIP file is empty\"}");
            goto finalResponse;
        }
        // Config.MaximumWipSize is set in megabytes, ContentLength header is in bytes
        if (headResponseMessage.Content.Headers.ContentLength >= Config.MaximumWipSize * 1048576)
        {
            response = Encoding.Default.GetBytes("{\"message\": \"WIP file exceeds maximum file size cap\"}");
            goto finalResponse;
        }

        byte[] keyResponse = AddWip(url,
            urlQuery.Get("user"),
            bool.Parse(urlQuery.Get("prepend") ?? "true"),
            urlQuery.Get("service"));

        code = 200;
        response = keyResponse;

        finalResponse:
            return new KeyValuePair<int, byte[]>(code, response);
    }

    private static KeyValuePair<int, byte[]> HandleBlacklistContext(string[] path)
    {
        int code = 400;
        byte[] response = Encoding.Default.GetBytes("{\"message\": \"Invalid request\"}");
        
        if (path.Length <= 2)
        {
            code = 200;
            response = BlacklistManager.GetEncoded();
            goto finalResponse;
        }

        string wantedKey = path[3].Replace("/", string.Empty);
        if (!long.TryParse(wantedKey, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long _))
        {
            response = Encoding.Default.GetBytes("{\"message\": \"Invalid key\"}");
            goto finalResponse;
        }

        switch (path[2].Replace("/", string.Empty).ToLower())
        {
            case "add":
                BlacklistManager.AddKey(wantedKey);
                
                code = 200;
                response = Encoding.Default.GetBytes("{\"message\": \"Blacklisted key " + wantedKey + "\"}");
                break;
            
            case "remove":
                BlacklistManager.RemoveKey(wantedKey);
                
                code = 200;
                response = Encoding.Default.GetBytes("{\"message\": \"Removed key " + wantedKey + " from the blacklist\"}");
                break;
        }
        
        finalResponse:
            return new KeyValuePair<int, byte[]>(code, response);
    }

    private static async Task HandleContext(HttpListenerContext context)
    {
        string[] path = context.Request.Url.Segments;
        NameValueCollection urlQuery = System.Web.HttpUtility.ParseQueryString(context.Request.Url.Query);
        
        Plugin.Log.Info($"GET request path: {string.Join(", ", path)}");
        
        KeyValuePair<int, byte[]> response;

        if (path.Length <= 1)
        {
            response = new KeyValuePair<int, byte[]>(200, Encoding.Default.GetBytes("{\"message\": \"Hello!\"}"));
        }
        else
        {
            switch (path[1].Replace("/", string.Empty).ToLower())
            {
                case "favicon.ico":
                    context.Response.StatusCode = 404;
                    context.Response.KeepAlive = false;
                    context.Response.ContentLength64 = 0;
                    context.Response.Close();
                    return;
                
                case "query":
                    response = await HandleQueryContext(path);
                    break;

                case "addkey":
                    var result = await AddMapRouter.HandleContext(context);
                    response = new KeyValuePair<int, byte[]>(result.StatusCode, Encoding.Default.GetBytes(result.Message));
                    break;

                case "addwip":
                    response = new KeyValuePair<int, byte[]>(400, Encoding.Default.GetBytes("{\"message\": \"Please use POST for this endpoint.\"}"));
                    if (context.Request.HttpMethod == "POST")
                    {
                        using StreamReader reader = new(context.Request.InputStream, context.Request.ContentEncoding);
                        response = await HandleAddLinkContext(await reader.ReadToEndAsync(), urlQuery);
                    }
                    break;

                case "queue":
                    result = await QueueRouter.HandleContext(context);
                    response = new KeyValuePair<int, byte[]>(result.StatusCode, Encoding.Default.GetBytes(result.Message));
                    break;

                case "history":
                    response = new KeyValuePair<int, byte[]>(200,
                        GetSessionHistory(int.Parse(urlQuery.Get("limit") ?? "0")));
                    break;
                
                case "version":
                    response = new KeyValuePair<int, byte[]>(200,
                        Encoding.Default.GetBytes(JsonConvert.SerializeObject(new VersionOutput())));
                    break;
                
                case "blacklist":
                    response = HandleBlacklistContext(path);
                    break;

                default:
                    response = new KeyValuePair<int, byte[]>(501,
                        Encoding.Default.GetBytes("{\"message\": \"Not implemented\"}"));
                    break;
            }
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.Key;
        context.Response.KeepAlive = false;
        context.Response.ContentLength64 = response.Value.Length;
        
        Stream outputStream = context.Response.OutputStream;
        await outputStream.WriteAsync(response.Value, 0, response.Value.Length);
        
        outputStream.Close();
        context.Response.Close();
    }
    
    private class TimestampComparer : IComparer
    {
        private readonly CaseInsensitiveComparer _comparer = new();
        public int Compare(object? x, object? y)
        {
            return _comparer.Compare(y, x);
        }
    }
    private static readonly TimestampComparer TimestampComparerInstance = new();

    [JsonObject(MemberSerialization.OptIn)]
    private struct HistorySpotItem(long timestamp, NoncontextualizedSong historyItem)
    {
        [JsonProperty] private int Timestamp => (int)timestamp;
        [JsonProperty] private NoncontextualizedSong HistoryItem => historyItem;
    }

    private static byte[] GetSessionHistory(int limit = 0)
    {
        long[] keys = SessionHistoryManager.SessionHistory.Keys.ToArray();
        Array.Sort(keys, TimestampComparerInstance);
        if (limit > 0)
        {
            limit = Math.Min(keys.Length, limit);
            keys = keys[0..limit];
        }

        HistorySpotItem[] historyItems = keys.Select(timestamp => new HistorySpotItem(timestamp, SessionHistoryManager.SessionHistory[timestamp])).ToArray();
        
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(historyItems));
    }

    [JsonObject(MemberSerialization.OptIn)]
    private struct QueueSpotItem(int spot, NoncontextualizedSong queueItem)
    {
        [JsonProperty] private int Spot => spot;
        [JsonProperty] private NoncontextualizedSong QueueItem => queueItem;
    }
    
    private static byte[] AddWip(string url, string? user = null, bool prepend = true, string? service = null)
    {
        Plugin.Log.Info($"Adding wip {url}...");
        NoncontextualizedSong queuedSong = QueueManager.AddWip(url, user, prepend, service);
        
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(queuedSong));
    }

    private static async Task<byte[]?> QuerySkipCache(string key)
    {
        Plugin.Log.Info($"Querying key {key} (directly)...");
        
        Beatmap? beatmap = await SongDetailsManager.GetDirectByKey(key);
        if (beatmap != null)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new NoncontextualizedSong(beatmap)));
        }

        Plugin.Log.Info($"No beatmap found for key {key}");
        return null;
    }

    private static async Task<byte[]?> Query(string key)
    {
        Plugin.Log.Info($"Querying key {key}...");

        NoncontextualizedSong queriedSong;
        
        CachedMap? song = SongDetailsManager.GetByKey(key);
        if (song == null)
        {
            Beatmap? beatmap = await SongDetailsManager.GetDirectByKey(key); 
            if (beatmap != null)
            {
                Plugin.DebugMessage("Using BeatSaverSharp method");
                queriedSong = new NoncontextualizedSong(beatmap);
            }
            else
            {
                Plugin.Log.Info("oh we're screwed");
                return null;
            }
        }
        else
        {
            string hash = song.Hash.ToLower();
            Plugin.DebugMessage($"Checking SongCore for hash {hash}");
            
            BeatmapLevel? beatmapLevel = SongCore.Loader.GetLevelByHash(hash);
            if (beatmapLevel != null)
            {
                Plugin.DebugMessage("Using local map method");
                queriedSong = new NoncontextualizedSong(beatmapLevel);
            }
            else
            {
                Plugin.DebugMessage("Using protobuf cache method");
                queriedSong = new NoncontextualizedSong(song);
            }
        }

        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(queriedSong));
    }
}