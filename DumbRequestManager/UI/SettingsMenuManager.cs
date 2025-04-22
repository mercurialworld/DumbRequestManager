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
    
    public void Initialize()
    {
#if V1_39_1
        BeatSaberMarkupLanguage.Settings.BSMLSettings.Instance.AddSettingsMenu("DumbRequestManager",
            "DumbRequestManager.UI.BSML.Settings.bsml", this);
#else
        BeatSaberMarkupLanguage.Settings.BSMLSettings.instance?.AddSettingsMenu("DumbRequestManager", "DumbRequestManager.UI.BSML.Settings.bsml", this);
#endif
    }

    public void Dispose()
    {
#if V1_39_1
        BeatSaberMarkupLanguage.Settings.BSMLSettings.Instance?.RemoveSettingsMenu(this);
#else
        BeatSaberMarkupLanguage.Settings.BSMLSettings.instance?.RemoveSettingsMenu(this);
#endif
    }
}