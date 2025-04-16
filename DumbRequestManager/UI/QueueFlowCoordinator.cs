using HMUI;
using JetBrains.Annotations;
using Zenject;

namespace DumbRequestManager.UI;

internal class QueueFlowCoordinator : FlowCoordinator
{
    private SoloFreePlayFlowCoordinator _soloFreePlayFlowCoordinator = null!;
    private QueueViewController _queueViewController = null!;
    
    [Inject]
    [UsedImplicitly]
    private void Construct(SoloFreePlayFlowCoordinator soloFreePlayFlowCoordinator, QueueViewController queueViewController)
    {
        _soloFreePlayFlowCoordinator = soloFreePlayFlowCoordinator;
        _queueViewController = queueViewController;
    }
    
    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        if (firstActivation)
        {
            SetTitle("Map Requests");
            showBackButton = true;
        }

        if (addedToHierarchy)
        {
            ProvideInitialViewControllers(_queueViewController);
        }
        
        _queueViewController.ReloadQueue();
    }

    // ReSharper disable once ParameterHidesMember
    protected override void BackButtonWasPressed(ViewController topViewController)
    {
        _soloFreePlayFlowCoordinator.DismissFlowCoordinator(this);
    }
}