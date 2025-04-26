using DumbRequestManager.Managers;
using HMUI;
using JetBrains.Annotations;
using Zenject;

namespace DumbRequestManager.UI;

internal class QueueFlowCoordinator : FlowCoordinator
{
    private SoloFreePlayFlowCoordinator _soloFreePlayFlowCoordinator = null!;
    private QueueViewController _queueViewController = null!;
    private SideSettingsViewController _sideSettingsViewController = null!;
    
    [Inject]
    [UsedImplicitly]
    private void Construct(SoloFreePlayFlowCoordinator soloFreePlayFlowCoordinator,
        QueueViewController queueViewController,
        SideSettingsViewController sideSettingsViewController)
    {
        _soloFreePlayFlowCoordinator = soloFreePlayFlowCoordinator;
        _queueViewController = queueViewController;
        _sideSettingsViewController = sideSettingsViewController;
        
        // calling it here
        _ = QueueManager.Load();
    }

    public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        if (firstActivation)
        {
            SetTitle("Map Requests");
            showBackButton = true;
        }

        if (addedToHierarchy)
        {
            ProvideInitialViewControllers(_queueViewController, _sideSettingsViewController);
        }
    }

    // ReSharper disable once ParameterHidesMember
    public override void BackButtonWasPressed(ViewController _)
    {
        _soloFreePlayFlowCoordinator.DismissFlowCoordinator(this);
    }
}