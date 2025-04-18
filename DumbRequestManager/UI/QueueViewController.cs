using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverDownloader.Misc;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using DumbRequestManager.Classes;
using DumbRequestManager.Managers;
using HMUI;
using IPA.Utilities.Async;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace DumbRequestManager.UI;

[ViewDefinition("DumbRequestManager.UI.BSML.QueueView.bsml")]
[HotReload(RelativePathToLayout = "BSML.QueueView.bsml")]
internal class QueueViewController : BSMLAutomaticViewController
{
    private LevelFilteringNavigationController _levelFilteringNavigationController = null!;
    private LevelCollectionViewController _levelCollectionViewController = null!;
    private SelectLevelCategoryViewController _selectLevelCategoryViewController = null!;
    
    private LoadingControl _loadingSpinner = null!;
    
    private static readonly BeatSaver BeatSaverInstance = new(nameof(DumbRequestManager), Assembly.GetExecutingAssembly().GetName().Version);
    
    [UIValue("queue")]
    private static List<QueuedSong> Queue => QueueManager.QueuedSongs;
    
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    [UIComponent("queueTableComponent")]
    private static CustomCellListTableData _queueTableComponent = null!;
    
    [UIComponent("waitModal")]
    public ModalView waitModal = null!;
    
    [UIComponent("loadingSpinnerContainer")]
    public VerticalLayoutGroup loadingSpinnerContainer = null!;
    
    [UIComponent("detailsCoverImage")]
    public ImageView detailsCoverImage = null!;
    
    [UIComponent("detailsTitle")]
    public TextMeshProUGUI detailsTitle = null!;
    [UIComponent("detailsArtist")]
    public TextMeshProUGUI detailsArtist = null!;
    [UIComponent("detailsMapper")]
    public TextMeshProUGUI detailsMapper = null!;

    [Inject]
    [UsedImplicitly]
    private void Construct(LevelFilteringNavigationController levelFilteringNavigationController, LevelCollectionViewController levelCollectionViewController, SelectLevelCategoryViewController selectLevelCategoryViewController)
    {
        _levelFilteringNavigationController = levelFilteringNavigationController;
        _levelCollectionViewController = levelCollectionViewController;
        _selectLevelCategoryViewController = selectLevelCategoryViewController;
    }
    
    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

        if (_queueTableComponent == null)
        {
            // i... don't know. this works.
            _queueTableComponent = GameObject.Find("QueueTableComponent").GetComponent<CustomCellListTableData>();
        }

        if (firstActivation)
        {
            _queueTableComponent.TableView.selectionType = TableViewSelectionType.Single;
            detailsCoverImage.material = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "UINoGlowRoundEdge");
        }

        _queueTableComponent.TableView.ReloadDataKeepingPosition();
    }

    [UIAction("selectCell")]
    public void SelectCell(TableView tableView, QueuedSong queuedSong)
    {
        int index = tableView._selectedCellIdxs.First();
        
        Plugin.Log.Info($"Selected cell: {index}");
        Plugin.Log.Info($"Selected song: {queuedSong.Artist} - {queuedSong.Title} [{queuedSong.Mapper}]");
        Plugin.Log.Info($"Cells: {tableView._contentTransform.childCount}");

        detailsTitle.text = queuedSong.Title;
        detailsArtist.text = queuedSong.Artist;
        detailsMapper.text = queuedSong.Mapper;

        UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
        {
            if (queuedSong.CoverImage == null)
            {
                detailsCoverImage.sprite = SongCore.Loader.defaultCoverImage;
                return;
            }
            
            detailsCoverImage.sprite = await Utilities.LoadSpriteAsync(queuedSong.CoverImage);
        });
    }
    
    public void GoToLevel(BeatmapLevel? beatmapLevel)
    {
        Plugin.Log.Info("GoToLevel called");

        if (beatmapLevel == null)
        {
            Plugin.Log.Info("beatmapLevel is null");
            return;
        }
        
        _levelFilteringNavigationController.UpdateCustomSongs();
        
        _selectLevelCategoryViewController.LevelFilterCategoryIconSegmentedControlDidSelectCell(
            _selectLevelCategoryViewController._levelFilterCategoryIconSegmentedControl, 1);
        
        Plugin.Log.Info($"Selecting {beatmapLevel.songName} in the map list...");
        _levelCollectionViewController._levelCollectionTableView.SelectLevel(beatmapLevel);
        Plugin.Log.Info("Should be selected");
    }

    public void OkGoBack(QueuedSong queuedSong)
    {
        waitModal.Hide(false);
        
        Plugin.Log.Info("Going back to the map list screen");
        GameObject.Find("QueueFlowCoordinator").GetComponent<QueueFlowCoordinator>().BackButtonWasPressed(this);
        try
        {
            GoToLevel(SongCore.Loader.GetLevelByHash(queuedSong.Hash));
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }

        Plugin.Log.Info("Should've selected level");
    }

    [UIAction("skipButtonPressed")]
    public void SkipButtonPressed()
    {
        int index = _queueTableComponent.TableView._selectedCellIdxs.First();
        if (index == -1)
        {
            Plugin.Log.Info("Nothing selected");
            return;
        }
        
        Plugin.Log.Info($"Selected cell: {index}");
        
        Queue.RemoveAt(index);
        _queueTableComponent.TableView.ClearSelection();
        _queueTableComponent.TableView.ReloadData();
        
        ChatRequestButton.Instance.UseAttentiveButton(Queue.Count > 0);
    }

    [UIAction("playButtonPressed")]
    public async Task PlayButtonPressed()
    {
        if (_loadingSpinner == null)
        {
            _loadingSpinner = Instantiate(Resources.FindObjectsOfTypeAll<LoadingControl>().First(), loadingSpinnerContainer.transform);
            _loadingSpinner.ShowLoading("Downloading..."); // figure out text later
            
#if DEBUG
            waitModal.Show(false);
#endif
        }
        
        int index = _queueTableComponent.TableView._selectedCellIdxs.First();
        if (index == -1)
        {
            Plugin.Log.Info("Nothing selected");
            return;
        }
        
        waitModal.Show(false);
        
        Plugin.Log.Info($"Selected cell: {index}");
        QueuedSong queuedSong = Queue[index];
        
        Plugin.Log.Info($"Selected song: {queuedSong.Artist} - {queuedSong.Title} [{queuedSong.Mapper}]");
        
        Queue.RemoveAt(index);
        _queueTableComponent.TableView.ClearSelection();
        _queueTableComponent.TableView.ReloadData();
        
        ChatRequestButton.Instance.UseAttentiveButton(Queue.Count > 0);
        
        Beatmap? beatmap = await BeatSaverInstance.Beatmap(queuedSong.BsrKey);
        if (beatmap != null)
        {
            Plugin.Log.Info("Beatmap was not null");
            
            if (!SongCore.Collections.songWithHashPresent(queuedSong.Hash))
            {
                Plugin.Log.Info("Beatmap doesn't exist locally, grabbing it");
                
                await SongDownloader.Instance.DownloadSong(beatmap, CancellationToken.None);
                
                SongCore.Loader.SongsLoadedEvent += LoaderOnSongsLoadedEvent;
                SongCore.Loader.Instance.RefreshSongs();
            }
            else
            {
                OkGoBack(queuedSong);
            }

            void LoaderOnSongsLoadedEvent(SongCore.Loader loader, ConcurrentDictionary<string, BeatmapLevel> concurrentDictionary)
            {
                SongCore.Loader.SongsLoadedEvent -= LoaderOnSongsLoadedEvent;
                OkGoBack(queuedSong);
            }
        }
    }
}