using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using DumbRequestManager.Classes;
using DumbRequestManager.Managers;

namespace DumbRequestManager.UI;

[ViewDefinition("DumbRequestManager.UI.BSML.QueueView.bsml")]
[HotReload(RelativePathToLayout = "BSML.QueueView.bsml")]
internal class QueueViewController : BSMLAutomaticViewController
{
    [UIValue("queue")] internal static List<QueuedSong> Queue => QueueManager.QueuedSongs;
    
    [UIComponent("queueTableComponent")]
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public static CustomCellListTableData? QueueTableComponent = null!;
    
    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
    }

    public static void ReloadQueue()
    {
        Plugin.Log.Info("Reloading queue view...");
        QueueTableComponent?.TableView.ReloadDataKeepingPosition();
    }
}