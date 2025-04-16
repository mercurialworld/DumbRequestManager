using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BeatSaverDownloader.Misc;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using DumbRequestManager.Managers;
using JetBrains.Annotations;
using Zenject;
using PluginConfig = DumbRequestManager.Configuration.PluginConfig;

namespace DumbRequestManager.Services;

[UsedImplicitly]
internal class HttpApi : IInitializable, IDisposable
{
    private static PluginConfig Config => PluginConfig.Instance;
    private static HttpListener? _httpListener;
    private static readonly BeatSaver BeatSaverInstance = new(nameof(DumbRequestManager), Assembly.GetExecutingAssembly().GetName().Version);

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

        switch (path[1][..^1])
        {
            case "addKey":
                AddKey(path[2]);
                break;
            
            case "downloadKey":
                Beatmap? beatmap = await BeatSaverInstance.Beatmap(path[2]);
                await SongDownloader.Instance.DownloadSong(beatmap, CancellationToken.None);
                SongCore.Loader.Instance.RefreshSongs();
                break;
        }
        
        byte[] data = "hello!"u8.ToArray();
        
        context.Response.StatusCode = 200;
        context.Response.KeepAlive = false;
        context.Response.ContentLength64 = data.Length;
        
        Stream outputStream = context.Response.OutputStream;
        await outputStream.WriteAsync(data, 0, data.Length);
        
        outputStream.Close();
        context.Response.Close();
    }

    private static void AddKey(string key)
    {
        Plugin.Log.Info($"Adding key {key}...");
        QueueManager.AddKey(key);
    }

    public void Dispose()
    {
        _keepListening = false;
        
        Plugin.Log.Info("Disposing HttpApi...");
        _httpListener?.Stop();
    }
}