using DumbRequestManager.Configuration;
using DumbRequestManager.Installers;
using IPA;
using IPA.Config.Stores;
using JetBrains.Annotations;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;
using IPAConfig = IPA.Config.Config;

namespace DumbRequestManager;

[Plugin(RuntimeOptions.DynamicInit)]
[NoEnableDisable]
[UsedImplicitly]
internal class Plugin
{
    internal static IPALogger Log { get; private set; } = null!;

    [Init]
    public Plugin(IPALogger ipaLogger, IPAConfig ipaConfig, Zenjector zenjector)
    {
        Log = ipaLogger;
        zenjector.UseLogger(Log);
        
        PluginConfig c = ipaConfig.Generated<PluginConfig>();
        PluginConfig.Instance = c;
        
        zenjector.Install<AppInstaller>(Location.App);
        zenjector.Install<MenuInstaller>(Location.Menu);
        
        Log.Info("Plugin loaded");
    }

    public static void DebugMessage(string message)
    {
#if DEBUG
        Log.Info(message);
#endif
    }
}