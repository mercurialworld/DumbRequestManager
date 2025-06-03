using System;
using BeatSaberMarkupLanguage.Attributes;
using DumbRequestManager.Configuration;
using JetBrains.Annotations;
using Zenject;

namespace DumbRequestManager.UI;

[UsedImplicitly]
internal class SettingsMenuManager : IInitializable, IDisposable
{
    private static PluginConfig Config => PluginConfig.Instance;

    [UsedImplicitly]
    [UIValue("whereIsItListening")]
    private static string WhereIsItListening => $"http://{Config.HttpAddress}:{Config.HttpPort}";
    
    [UsedImplicitly]
    [UIValue("whereIsItAlsoListening")]
    private static string WhereIsItAlsoListening => $"ws://{Config.WebSocketAddress}:{Config.WebSocketPort}";
    
    [UsedImplicitly]
    protected bool PlayAudioPreviews
    {
        get => Config.PlayAudioPreviews;
        set => Config.PlayAudioPreviews = value;
    }
    
    [UsedImplicitly]
    protected bool PlayRemoteAudioPreviews
    {
        get => Config.PlayRemoteAudioPreviews;
        set => Config.PlayRemoteAudioPreviews = value;
    }
    
    public void Initialize()
    {
        BeatSaberMarkupLanguage.Settings.BSMLSettings.Instance.AddSettingsMenu("DumbRequestManager",
            "DumbRequestManager.UI.BSML.Settings.bsml", this);
    }

    public void Dispose()
    {
        BeatSaberMarkupLanguage.Settings.BSMLSettings.Instance?.RemoveSettingsMenu(this);
    }
}