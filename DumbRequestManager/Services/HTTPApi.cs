using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
//using System.Reflection;
using System.Threading.Tasks;
using BeatSaverSharp.Models;
//using BeatSaverSharp;
using DumbRequestManager.Classes;
using DumbRequestManager.Managers;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SongDetailsCache.Structs;
using Zenject;
using PluginConfig = DumbRequestManager.Configuration.PluginConfig;

namespace DumbRequestManager.Services;

[UsedImplicitly]
internal class HttpApi : IInitializable, IDisposable
{
    private static PluginConfig Config => PluginConfig.Instance;
    private static HttpListener? _httpListener;
    //private static readonly BeatSaver BeatSaverInstance = new(nameof(DumbRequestManager), Assembly.GetExecutingAssembly().GetName().Version);

    private static bool _keepListening = true;
    
    [SuppressMessage("ReSharper", "FunctionNeverReturns")]
    public void Initialize()
    {
        Plugin.Log.Info("Initializing HttpApi...");
        _httpListener = new HttpListener
        {
            Prefixes = { $"http://localhost:{Config.HttpPort}/" }
        };
        
        _httpListener.Start();
        Plugin.Log.Info("HttpApi started");
        _ = Task.Run(async () =>
        {
            while (_keepListening)
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
        });
    }

    private static async Task HandleContext(HttpListenerContext context)
    {
        string[] path = context.Request.Url.Segments;
        if (path[1] == "favicon.ico")
        {
            context.Response.StatusCode = 404;
            context.Response.KeepAlive = false;
            context.Response.ContentLength64 = 0;
            context.Response.Close();
            return;
        }
        
#if DEBUG
        Plugin.Log.Info($"path: {string.Join(", ", path)}");
#endif

        byte[] failedData = "{\"message\": \"Not implemented\"}"u8.ToArray();
        byte[] data = failedData;
        int statusCode = 501;
        
        switch (path[1][..^1])
        {
            case "query":
                byte[]? queryResponse = await Query(path[2]);
                if (queryResponse != null)
                {
                    statusCode = 200;
                    data = queryResponse;
                }
                else
                {
                    statusCode = 400;
                    data = "{\"message\": \"Invalid request\"}"u8.ToArray();
                }
                break;
            
            case "addKey":
                byte[]? keyResponse = await AddKey(path[2]);
                if (keyResponse != null)
                {
                    statusCode = 200;
                    data = keyResponse;
                }
                else
                {
                    statusCode = 400;
                    data = "{\"message\": \"Invalid request\"}"u8.ToArray();
                }
                break;
        }
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        context.Response.KeepAlive = false;
        context.Response.ContentLength64 = data.Length;
        
        Stream outputStream = context.Response.OutputStream;
        await outputStream.WriteAsync(data, 0, data.Length);
        
        outputStream.Close();
        context.Response.Close();
    }

    private static async Task<byte[]?> AddKey(string key)
    {
        Plugin.Log.Info($"Adding key {key}...");
        NoncontextualizedSong? queuedSong = await QueueManager.AddKey(key);
        
        return queuedSong == null
            ? null
            : System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(queuedSong));
    }

    private static async Task<byte[]?> Query(string key)
    {
        Plugin.Log.Info($"Querying key {key}...");
        
        /*Object song = SongDetailsManager.GetByKey(key);
        
        NoncontextualizedSong queriedSong =
            song is Song song1 ? new NoncontextualizedSong(song1) : new NoncontextualizedSong((Beatmap)song);*/

        NoncontextualizedSong queriedSong;
        
        Song? song = SongDetailsManager.GetByKey(key);
        if (song == null)
        {
            Beatmap? beatmap = await SongDetailsManager.GetDirectByKey(key); 
            if (beatmap != null)
            {
                queriedSong = new NoncontextualizedSong(beatmap);
            }
            else
            {
                return null;
            }
        }
        else
        {
            queriedSong = new NoncontextualizedSong(song);
        }

        return System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(queriedSong));
    }

    public void Dispose()
    {
        _keepListening = false;
        
        Plugin.Log.Info("Disposing HttpApi...");
        _httpListener?.Stop();
    }
}