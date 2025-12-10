using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DumbRequestManager.API.Models;
using DumbRequestManager.API.Routes;
using DumbRequestManager.Classes;
using DumbRequestManager.Managers;
using IPA.Utilities;
using JetBrains.Annotations;
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

    private static async Task HandleContext(HttpListenerContext context)
    {
        string[] path = context.Request.Url.Segments;
        string endpoint = path[1].Replace("/", string.Empty).ToLower();
        
        Plugin.Log.Info($"GET request path: {string.Join(", ", path)}");
        
        APIResponse response = new();

        if (path.Length <= 1)
        {
            response.Set(HttpStatusCode.OK, APIResponse.APIMessage("Hello!"));
        }
        else
        {
            switch (endpoint)
            {
                case "favicon.ico":
                    context.Response.StatusCode = 404;
                    context.Response.KeepAlive = false;
                    context.Response.ContentLength64 = 0;
                    context.Response.Close();
                    return;
                
                case "query":
                    response = await QueryRouter.HandleContext(context);
                    break;

                case "addkey":
                    response = await AddMapRouter.HandleContext(context);
                    break;
                
                case "addwip":
                    response = await AddWIPRouter.HandleContext(context);
                    break;

                case "queue":
                    response = await QueueRouter.HandleContext(context);
                    break;

                case "history":
                    response =  await HistoryRouter.HandleContext(context);
                    break;
                
                case "version":
                    response = await VersionRouter.HandleContext();
                    break;
                
                case "blacklist":
                    response = await BlacklistRouter.HandleContext(context);
                    break;

                default:
                    response.Set(HttpStatusCode.NotImplemented, APIResponse.APIMessage("Not implemented"));
                    break;
            }
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.StatusCode;
        context.Response.KeepAlive = false;
        context.Response.ContentLength64 = response.PayloadInBytes.Length;
        
        Stream outputStream = context.Response.OutputStream;
        await outputStream.WriteAsync(response.PayloadInBytes, 0, response.PayloadInBytes.Length);
        
        outputStream.Close();
        context.Response.Close();
    }
}