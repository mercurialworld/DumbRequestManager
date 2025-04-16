using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using DumbRequestManager.Classes;
using DumbRequestManager.Managers;
using UnityEngine.Serialization;

namespace DumbRequestManager.UI;

[ViewDefinition("DumbRequestManager.UI.BSML.QueueView.bsml")]
[HotReload(RelativePathToLayout = "BSML.QueueView.bsml")]
internal class QueueViewController : BSMLAutomaticViewController
{
    [UIValue("queue")] internal static List<QueuedSong> Queue => QueueManager.QueuedSongs;
    
    [UIComponent("queueTableComponent")]
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public CustomCellListTableData? queueTableComponent = null!;
    
    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
    }

    public void ReloadQueue()
    {
        Plugin.Log.Info("Reloading queue view...");
        queueTableComponent?.TableView.ReloadDataKeepingPosition();
    }
}