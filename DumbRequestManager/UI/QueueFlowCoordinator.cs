using System.Collections.Concurrent;
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
        
        SongCore.Loader.SongsLoadedEvent += EventLoadThing;
    }
    
    private static void EventLoadThing(SongCore.Loader loader, ConcurrentDictionary<string, BeatmapLevel> concurrentDictionary)
    {
        _ = QueueManager.Load();
        SongCore.Loader.SongsLoadedEvent -= EventLoadThing;
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