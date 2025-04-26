using DumbRequestManager.UI;
using JetBrains.Annotations;
using Zenject;

namespace DumbRequestManager.Installers;

[UsedImplicitly]
internal class MenuInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<SettingsMenuManager>().AsSingle();
        
        Container.Bind<QueueViewController>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<SideSettingsViewController>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<QueueFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
        Container.BindInterfacesTo<ChatRequestButton>().AsSingle();
    }
}