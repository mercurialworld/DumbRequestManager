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
        Container.BindInterfacesTo<HttpApi>().AsSingle();
        
        Container.BindInterfacesTo<SongDetailsManager>().AsSingle();
    }
}