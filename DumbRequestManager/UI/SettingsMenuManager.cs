using System;
using System.ComponentModel;
using DumbRequestManager.Configuration;
using JetBrains.Annotations;
using Zenject;

namespace DumbRequestManager.UI;

[UsedImplicitly]
internal class SettingsMenuManager : IInitializable, IDisposable
{
    private static PluginConfig Config => PluginConfig.Instance;

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