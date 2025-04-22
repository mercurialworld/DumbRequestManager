using System;
using DumbRequestManager.Configuration;
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
    
    public void Initialize()
    {
        _webSocketServer = new WebSocketServer($"ws://{Config.WebSocketAddress}:{Config.WebSocketPort}");
        _webSocketServer.AddWebSocketService<SocketMessageHandler>("/");

        if (_webSocketServer.WebSocketServices.TryGetServiceHost("/", out WebSocketServiceHost webSocketServiceHost))
        {
            _webSocketServiceHost = webSocketServiceHost;
        }
            
        _webSocketServer.Start();
    }

    public void Dispose()
    {
        _webSocketServer?.Stop();
    }
}

internal class SocketMessageHandler : WebSocketBehavior
{
    protected override void OnOpen()
    {
        Plugin.DebugMessage("Connection opened");
    }
    protected override void OnClose(CloseEventArgs e)
    {
        Plugin.DebugMessage("Connected closed");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        // do nothing, this is just a firehose
    }
}