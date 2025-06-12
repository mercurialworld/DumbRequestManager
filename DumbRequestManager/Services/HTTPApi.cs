using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BeatSaverSharp.Models;
using DumbRequestManager.Classes;
using DumbRequestManager.Managers;
using DumbRequestManager.UI;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Zenject;
using PluginConfig = DumbRequestManager.Configuration.PluginConfig;

namespace DumbRequestManager.Services;

[UsedImplicitly]
internal class HttpApi : IInitializable
{
    private static PluginConfig Config => PluginConfig.Instance;

    private static HttpListener? _httpListener;
    
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

    private static async Task<KeyValuePair<int, byte[]>> HandleAddKeyContext(string[] path,
        NameValueCollection urlQuery, bool isWip = false)
    {
        int code = 400;
        byte[] response = Encoding.Default.GetBytes("{\"message\": \"Invalid request\"}");

        if (path.Length <= 2)
        {
            goto finalResponse;
        }

        if (!int.TryParse(path.Last().Replace("/", string.Empty).ToLower(), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out int _))
        {
            goto finalResponse;
        }

        byte[]? keyResponse;
        if (isWip)
        {
            keyResponse = AddWip(path.Last().Replace("/", string.Empty),
                urlQuery.Get("user"),
                bool.Parse(urlQuery.Get("prepend") ?? "true"));
        }
        else
        {
            keyResponse = await AddKey(path.Last().Replace("/", string.Empty),
                urlQuery.Get("user"),
                bool.Parse(urlQuery.Get("prepend") ?? "false"));
        }

        if (keyResponse != null)
        {
            code = 200;
            response = keyResponse;
        }

        finalResponse:
            return new KeyValuePair<int, byte[]>(code, response);
    }

    private static async Task<KeyValuePair<int, byte[]>> HandleQueueContext(string[] path)
    {
        int code = 400;
        byte[] response = Encoding.Default.GetBytes("{\"message\": \"Invalid request\"}");

        if (path.Length <= 2)
        {
            code = 200;
            response = GetEncodedQueue;
            goto finalResponse;
        }

        switch (path[2].Replace("/", string.Empty).ToLower())
        {
            case "where":
                code = 200;
                response = GetPositionsInQueue(path.Last().Replace("/", string.Empty));
                break;
                        
            case "clear":
                code = 200;
                            
                QueueManager.QueuedSongs.Clear();
                            
                QueueViewController.RefreshQueue();
                ChatRequestButton.Instance.UseAttentiveButton(false);
                            
                response = Encoding.Default.GetBytes("{\"message\": \"Queue cleared\"}");
                break;
                        
            case "open":
                if (path.Length <= 3)
                {
                    break;
                }

                if (bool.TryParse(path[3].Replace("/", string.Empty), out bool openResult))
                {
                    code = 200;
                    response = Encoding.Default.GetBytes("{\"message\": \"Queue gate changed\"}");
                    await SideSettingsViewController.Instance.SetState(openResult);
                }
                break;
                        
            case "move":
                if (path.Length != 5)
                {
                    break;
                }
                            
                const int min = 0;
                int max = QueueManager.QueuedSongs.Count - 1;

                if (!int.TryParse(path[3].Replace("/", string.Empty), out int targetedIndex))
                {
                    break;
                }
                targetedIndex--;

                int newIndex;
                if (!int.TryParse(path[4].Replace("/", string.Empty), out int newerIndex))
                {
                    switch (path[4].Replace("/", string.Empty).ToLower())
                    {
                        case "top": newIndex = 0; break;
                        case "bottom": newIndex = max; break;
                        default: goto finalResponse;
                    }
                }
                else
                {
                    newIndex = newerIndex - 1;
                }
                            
                if (targetedIndex < min || targetedIndex > max || newIndex < min || newIndex > max)
                {
                    break;
                }
                            
                NoncontextualizedSong oldSong = QueueManager.QueuedSongs[targetedIndex];
                QueueManager.QueuedSongs.RemoveAt(targetedIndex);
                QueueManager.QueuedSongs.Insert(newIndex, oldSong);

                QueueViewController.RefreshQueue();
                            
                code = 200;
                response = Encoding.Default.GetBytes("{\"message\": \"Moved entry\"}");
                break;
                        
            case "shuffle":
                code = 200;
                response = Encoding.Default.GetBytes("{\"message\": \"Queue shuffled\"}");
                            
                QueueManager.Shuffle();
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
                    response = await HandleAddKeyContext(path, urlQuery);
                    break;

                case "addwip":
                    response = await HandleAddKeyContext(path, urlQuery, true);
                    break;

                case "queue":
                    response = await HandleQueueContext(path);
                    break;

                case "history":
                    response = new KeyValuePair<int, byte[]>(200,
                        GetSessionHistory(int.Parse(urlQuery.Get("limit") ?? "0")));
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
    
    private static byte[] GetPositionsInQueue(string user)
    {
        int index = 0;
        List<QueueSpotItem> relevantQueueItems = [];
        
        QueueManager.QueuedSongs.ForEach(x =>
        {
            index++;

            if (x.User == user)
            {
                relevantQueueItems.Add(new QueueSpotItem(index, x));
            }
        });
        
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(relevantQueueItems));
    }

    private static byte[] GetEncodedQueue => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(QueueManager.QueuedSongs));

    private static async Task<byte[]?> AddKey(string key, string? user = null, bool prepend = false)
    {
        Plugin.Log.Info($"Adding key {key}...");
        NoncontextualizedSong? queuedSong = await QueueManager.AddKey(key, user, false, prepend);
        
        return queuedSong == null ? null : Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(queuedSong));
    }

    private static byte[] AddWip(string key, string? user = null, bool prepend = true)
    {
        Plugin.Log.Info($"Adding wip {key}...");
        NoncontextualizedSong queuedSong = QueueManager.AddWip(key, user, prepend);
        
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