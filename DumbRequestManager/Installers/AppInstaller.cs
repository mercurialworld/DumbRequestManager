using DumbRequestManager.Managers;
using DumbRequestManager.Services;
using JetBrains.Annotations;
using Zenject;

namespace DumbRequestManager.Installers;

[UsedImplicitly]
internal class AppInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<Utils.DownloaderUtils>().AsSingle();
        Container.BindInterfacesAndSelfTo<Utils.BeatLeaderUtils>().AsSingle();
        
        Container.BindInterfacesTo<HttpApi>().AsSingle();
        Container.BindInterfacesTo<SocketApi>().AsSingle();
        
        Container.BindInterfacesTo<MapCacheManager>().AsSingle();
        Container.BindInterfacesTo<SongDetailsManager>().AsSingle();
    }
}