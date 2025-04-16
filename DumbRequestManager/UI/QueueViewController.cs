using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverDownloader.Misc;
using DumbRequestManager.Classes;
using DumbRequestManager.Managers;
using HMUI;
using UnityEngine;

namespace DumbRequestManager.UI;

[ViewDefinition("DumbRequestManager.UI.BSML.QueueView.bsml")]
[HotReload(RelativePathToLayout = "BSML.QueueView.bsml")]
internal class QueueViewController : BSMLAutomaticViewController
{
    [UIValue("queue")] internal static List<QueuedSong> Queue => QueueManager.QueuedSongs;

    [UIComponent("queueTableComponent")]
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public CustomCellListTableData? queueTableComponent;
    
    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        
        if (queueTableComponent != null)
        {
            queueTableComponent.TableView.selectionType = TableViewSelectionType.Single;
        }
    }

    public void ReloadQueue()
    {
        Plugin.Log.Info("Reloading queue view...");

        queueTableComponent?.TableView.ClearHighlights();
        queueTableComponent?.TableView.ClearSelection();
        for (int i = 0; i < queueTableComponent?.TableView.numberOfCells; i++)
        {
            TableCell? cell = queueTableComponent.TableView.GetCellAtIndex(i);
            queueTableComponent?.TableView.DequeueReusableCellForIdentifier(cell?.reuseIdentifier);
        }
        
        queueTableComponent?.TableView.RefreshTable();
    }

    [UIAction("selectCell")]
    public void SelectCell(TableView tableView, QueuedSong queuedSong)
    {
        int index = tableView._selectedCellIdxs.First();
        Plugin.Log.Info($"Selected cell: {index}");
        tableView.ClearHighlights();

        TableCell selectedCell = tableView.GetCellAtIndex(index);
        selectedCell.highlighted = true;
        
        Plugin.Log.Info($"Cells: {tableView._contentTransform.childCount}");
        for (int i = 0; i < tableView._contentTransform.childCount; i++)
        {
            Transform? selectedRootObject = tableView._contentTransform.GetChild(i);
            if (selectedRootObject == null)
            {
                continue;
            }
            
            ImageView? imageView = selectedRootObject.GetComponentInChildren<ImageView>();
            if (imageView == null)
            {
                continue;
            }
            
            string wantedMaterial = index == i ? "AnimatedButton" : "UIFogBG";
            imageView.material = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == wantedMaterial);
            
            imageView.gradient = index == i;
            imageView._gradientDirection = ImageView.GradientDirection.Horizontal;
            
            imageView.color = index == i ? new Color(0f, 192f/255f, 1f) : Color.gray;
            imageView.color0 = index == i ? new Color(1f, 1f, 1f, 0f) : Color.white;
            imageView.color1 = Color.white;

            if (index == i)
            {
                imageView.__Refresh();
            }
        }
    }
}