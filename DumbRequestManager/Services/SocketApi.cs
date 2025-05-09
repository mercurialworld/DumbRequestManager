using System;
using System.Reflection;
using DumbRequestManager.Configuration;
using JetBrains.Annotations;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;
using Zenject;

namespace DumbRequestManager.Services;

[JsonObject(MemberSerialization.OptIn)]
internal class Message(string eventType, object? obj = null)
{
    [JsonProperty] private long Timestamp => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    [JsonProperty] private string EventType => eventType;
    [JsonProperty] private object? Data => obj;
}

[UsedImplicitly]
internal class SocketApi : IInitializable, IDisposable
{
    private static PluginConfig Config => PluginConfig.Instance;
    private static WebSocketServer? _webSocketServer;
    private static WebSocketServiceHost? _webSocketServiceHost;

    public static void Broadcast(string eventType, object? obj = null)
    {
        _webSocketServiceHost?.Sessions.Broadcast(JsonConvert.SerializeObject(new Message(eventType, obj)));
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
        
        _webSocketServer.Start();
        Plugin.DebugMessage("WebSocket server started");
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