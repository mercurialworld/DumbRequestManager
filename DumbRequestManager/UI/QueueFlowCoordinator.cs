using System.Collections.Concurrent;
using DumbRequestManager.Managers;
using HMUI;
using JetBrains.Annotations;
using Zenject;

namespace DumbRequestManager.UI;

internal class QueueFlowCoordinator : FlowCoordinator
{
    private MainFlowCoordinator _mainFlowCoordinator = null!;
    private QueueViewController _queueViewController = null!;
    private SideSettingsViewController _sideSettingsViewController = null!;
    private SongPreviewPlayer _songPreviewPlayer = null!;
    
    [Inject]
    [UsedImplicitly]
    private void Construct(MainFlowCoordinator mainFlowCoordinator,
        QueueViewController queueViewController,
        SideSettingsViewController sideSettingsViewController,
        SongPreviewPlayer songPreviewPlayer)
    {
        _mainFlowCoordinator = mainFlowCoordinator;
        _queueViewController = queueViewController;
        _sideSettingsViewController = sideSettingsViewController;
        _songPreviewPlayer = songPreviewPlayer;
        
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
#if !DEBUG
        QueueManager.Save();
#endif
        _songPreviewPlayer.CrossfadeToDefault();
        
        _mainFlowCoordinator.childFlowCoordinator.DismissFlowCoordinator(this);
    }
}