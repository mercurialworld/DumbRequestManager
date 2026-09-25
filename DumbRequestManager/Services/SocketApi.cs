using System;
using System.Reflection;
using DumbRequestManager.Configuration;
using DumbRequestManager.Services.API.Models;
using JetBrains.Annotations;
using WebSocketSharp;
using WebSocketSharp.Server;
using Zenject;

namespace DumbRequestManager.Services;

[UsedImplicitly]
internal class SocketApi : IInitializable, IDisposable
{
    private static PluginConfig Config => PluginConfig.Instance;
    private static WebSocketServer? _webSocketServer;
    private static WebSocketServiceHost? _webSocketServiceHost;

    public static void Broadcast(string eventType, object? obj = null)
    {
        _webSocketServiceHost?.Sessions.Broadcast(new SocketMessage(eventType, obj).ToString());
    }
    
    public void Initialize()
    {
        _webSocketServer = new WebSocketServer($"ws://{Config.WebSocketAddress}:{Config.WebSocketPort}");
        _webSocketServer.AddWebSocketService<SocketMessageHandler>("/");

        if (Enum.TryParse("6", out LogLevel logLevel))
        {
            Plugin.DebugMessage("LogLevel.None exists");
            _webSocketServer.Log.Level = logLevel;
        }
        else
        {
            Plugin.DebugMessage("LogLevel.None does not exist");
            FieldInfo? field = _webSocketServer.Log.GetType().GetField("_output", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(_webSocketServer.Log, new Action<LogData, string>((_, _) => { }));
        }

        if (_webSocketServer.WebSocketServices.TryGetServiceHost("/", out WebSocketServiceHost webSocketServiceHost))
        {
            _webSocketServiceHost = webSocketServiceHost;
        }

        try
        {
            _webSocketServer.Start();
            Plugin.Log.Info("WebSocket firehose started");
        }
        catch (InvalidOperationException)
        {
            Plugin.Log.Warn($"Unable to start WebSocket firehose on {Config.WebSocketAddress}:{Config.WebSocketPort}. More than likely, this port is already being used on this address.");
        }
    }

    public void Dispose()
    {
        _webSocketServer?.Stop();
        Plugin.DebugMessage("WebSocket server stopped");
    }
}

internal class SocketMessageHandler : WebSocketBehavior
{
    public SocketMessageHandler()
    {
        IgnoreExtensions = true;
    }
    
    protected override void OnOpen()
    {
        Plugin.DebugMessage("Connection opened");
        base.OnOpen();
    }
    protected override void OnClose(CloseEventArgs e)
    {
        Plugin.DebugMessage("Connection closed");
        base.OnClose(e);
    }

    protected override void OnError(ErrorEventArgs e)
    {
    }
}