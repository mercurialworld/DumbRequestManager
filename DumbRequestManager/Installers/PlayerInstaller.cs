using DumbRequestManager.Managers;
using JetBrains.Annotations;
using Zenject;

namespace DumbRequestManager.Installers;

[UsedImplicitly]
internal class PlayerInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<StartMapEvent>().AsSingle();
    }
}