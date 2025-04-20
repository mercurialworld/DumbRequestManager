using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaverDownloader.Misc;
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

public readonly struct CharacteristicUICellWrapper(string name, string icon)
{
    [UIValue("icon")] public string Icon => icon;
    [UIValue("name")] public string Name => name;
}

public readonly struct DifficultyUICellWrapper(NoncontextualizedDifficulty difficulty)
{
    [UIValue("name")] public string Name => Utils.Normalize.GetDifficultyName(difficulty.Difficulty);
    public float NotesPerSecond => difficulty.NotesPerSecond;
    public float NoteJumpSpeed => difficulty.NoteJumpSpeed;
}

[ViewDefinition("DumbRequestManager.UI.BSML.QueueView.bsml")]
[HotReload(RelativePathToLayout = "BSML.QueueView.bsml")]
internal class QueueViewController : BSMLAutomaticViewController
{
    private LevelFilteringNavigationController _levelFilteringNavigationController = null!;
    private LevelCollectionViewController _levelCollectionViewController = null!;
    private SelectLevelCategoryViewController _selectLevelCategoryViewController = null!;
    
    private LoadingControl _loadingSpinner = null!;
    
    [UIValue("queue")]
    private static List<NoncontextualizedSong> Queue => QueueManager.QueuedSongs;
    
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
    [UIComponent("detailsRequester")]
    public TextMeshProUGUI detailsRequester = null!;
    [UIComponent("detailsBsrKey")]
    public TextMeshProUGUI detailsBsrKey = null!;
    [UIComponent("detailsUploadDate")]
    public TextMeshProUGUI detailsUploadDate = null!;
    [UIComponent("detailsDescription")]
    public TextMeshProUGUI detailsDescription = null!;

    // ReSharper disable FieldCanBeMadeReadOnly.Global
    [UIComponent("detailsNotesPerSecond")]
    private static TextMeshProUGUI _detailsNps = null!;
    [UIComponent("detailsNoteJumpSpeed")]
    private static TextMeshProUGUI _detailsNjs = null!;
    // ReSharper restore FieldCanBeMadeReadOnly.Global
    
    [UIValue("difficultyChoices")]
    private static List<DifficultyUICellWrapper> _difficultyChoices = [];
    [UIValue("characteristicChoices")]
    private static List<CharacteristicUICellWrapper> _characteristicChoices = [];
    
    [UIValue("selectCharacteristicComponent")]
    private static CustomCellListTableData _selectCharacteristicComponent = null!;
    [UIValue("selectDifficultyComponent")]
    private static CustomCellListTableData _selectDifficultyComponent = null!;

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
            _selectCharacteristicComponent = GameObject.Find("DRM_SelectCharacteristicComponent").GetComponent<CustomCellListTableData>();
            _selectDifficultyComponent = GameObject.Find("DRM_SelectDifficultyComponent").GetComponent<CustomCellListTableData>();
            _detailsNps = GameObject.Find("DRM_DetailsNotesPerSecond").GetComponent<TextMeshProUGUI>();
            _detailsNjs = GameObject.Find("DRM_DetailsNoteJumpSpeed").GetComponent<TextMeshProUGUI>();
        }

        if (firstActivation)
        {
            _queueTableComponent.TableView.selectionType = TableViewSelectionType.Single;
            _selectCharacteristicComponent.TableView.selectionType = TableViewSelectionType.Single;
            _selectDifficultyComponent.TableView.selectionType = TableViewSelectionType.Single;
            
            _selectCharacteristicComponent.Data = _characteristicChoices;
            _selectDifficultyComponent.Data = _difficultyChoices;
            
            _selectCharacteristicComponent.TableView.didSelectCellWithIdxEvent += DidSelectCharacteristicCellWithIdxEvent;
            _selectDifficultyComponent.TableView.didSelectCellWithIdxEvent += DidSelectDifficultyCellWithIdxEvent;
            
            detailsCoverImage.material = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "UINoGlowRoundEdge");
        }

        _queueTableComponent.TableView.ReloadDataKeepingPosition();
    }

    private static readonly Color SelectedColor = Color.white;
    private static readonly Color StandardColor = new Color(1, 1, 1, 0.5f);
    private static void DidSelectCharacteristicCellWithIdxEvent(TableView tableView, int idx)
    {
        int childIdx = -1;
        for (int checkedIdx = 0; checkedIdx < tableView.contentTransform.transform.childCount; checkedIdx++)
        {
            Transform child = tableView.contentTransform.transform.GetChild(checkedIdx);
            if(!child.gameObject.activeSelf)
            {
                // not active, no need to consider it yet
                continue;
            }

            childIdx++;
    
            if (child.FindChildRecursively("BSMLImage").gameObject.TryGetComponent(out ImageView icon))
            {
                icon.color = childIdx == idx ? SelectedColor : StandardColor;
            }
        }
        
        CharacteristicUICellWrapper characteristic = _characteristicChoices[idx];
        _difficultyChoices = _selectedSong.Diffs.Where(x => x.Characteristic == characteristic.Name).Select(x => new DifficultyUICellWrapper(x)).ToList();

        Plugin.DebugMessage($"Got {_difficultyChoices.Count} unique difficulties");
        
        _selectDifficultyComponent.Data = _difficultyChoices;

        _selectDifficultyComponent.TableView.ReloadData();
        Plugin.DebugMessage("Reloaded characteristics/difficulties UI");

        _selectDifficultyComponent.TableView.SelectCellWithIdx(_selectDifficultyComponent.NumberOfCells() - 1, true);
        Plugin.DebugMessage("Should have selected difficulty");
    }
    
    private static void DidSelectDifficultyCellWithIdxEvent(TableView tableView, int idx)
    {
        int childIdx = -1;
        for (int checkedIdx = 0; checkedIdx < tableView.contentTransform.transform.childCount; checkedIdx++)
        {
            Transform child = tableView.contentTransform.transform.GetChild(checkedIdx);
            if(!child.gameObject.activeSelf)
            {
                // not active, no need to consider it yet
                continue;
            }

            childIdx++;
    
            if (child.FindChildRecursively("BSMLText").gameObject.TryGetComponent(out FormattableText text))
            {
                text.color = childIdx == idx ? SelectedColor : StandardColor;
            }
        }
        
        DifficultyUICellWrapper difficulty = _difficultyChoices[idx];
        Plugin.DebugMessage($"Selected difficulty {difficulty.Name}");
        
        // tried variables, failed miserably
        _detailsNjs.SetText($"{difficulty.NoteJumpSpeed:0.##} <size=80%><alpha=#AA>NJS");
        _detailsNps.SetText($"{difficulty.NotesPerSecond:0.00} <size=80%><alpha=#AA>NPS");
    }

    [UIAction("fetchDescription")]
    private static async Task<string?> FetchDescription(string bsrKey)
    {
        Beatmap? beatmap = await SongDetailsManager.BeatSaverInstance.Beatmap(bsrKey);
        if (beatmap != null)
        {
            return beatmap.Description;
        }
        
        Plugin.Log.Info("Beatmap fetch failed");
        return null;
    }

    private static NoncontextualizedSong _selectedSong = null!;
    [UIAction("selectCell")]
    public void SelectCell(TableView tableView, NoncontextualizedSong queuedSong)
    {
        _selectedSong = queuedSong;
        int index = tableView._selectedCellIdxs.First();
        
        Plugin.DebugMessage($"Selected cell: {index}");
        Plugin.DebugMessage($"Selected song: {queuedSong.Artist} - {queuedSong.Title} [{queuedSong.Mapper}]");
        Plugin.DebugMessage($"Cells: {tableView._contentTransform.childCount}");

        detailsTitle.text = queuedSong.Title;
        detailsArtist.text = queuedSong.Artist;
        detailsMapper.text = queuedSong.Mapper;
        detailsRequester.text = queuedSong.User ?? "someone";
        
        detailsBsrKey.text = $"<alpha=#AA>!bsr <alpha=#FF>{queuedSong.BsrKey}";
        
        DateTimeOffset uploadOffset = DateTimeOffset.FromUnixTimeSeconds(queuedSong.UploadTime);
        detailsUploadDate.text = uploadOffset.LocalDateTime.ToString("d MMM yyyy");
        
        _selectCharacteristicComponent.TableView.ClearSelection();
        _selectDifficultyComponent.TableView.ClearSelection();
        Plugin.DebugMessage("Cleared characteristics/difficulties");
        
        List<string> characteristics = [];
        foreach (NoncontextualizedDifficulty diff in queuedSong.Diffs)
        {
            if (characteristics.Contains(diff.Characteristic))
            {
                continue;
            }
            characteristics.Add(diff.Characteristic);
        }
        Plugin.DebugMessage($"Got {characteristics.Count} unique characteristics");
        _characteristicChoices = characteristics.Select(x => new CharacteristicUICellWrapper(x, Utils.Normalize.GetCharacteristicIcon(x))).ToList();
        Plugin.DebugMessage("Updated characteristic choices");
        _difficultyChoices = queuedSong.Diffs.Where(x => x.Characteristic == _characteristicChoices[0].Name).Select(x => new DifficultyUICellWrapper(x)).ToList();
        Plugin.DebugMessage($"Got {_difficultyChoices.Count} unique difficulties");
        
        _selectCharacteristicComponent.Data = _characteristicChoices;
        //_selectDifficultyComponent.Data = DifficultyChoices;
        
        _selectCharacteristicComponent.TableView.ReloadData();
        //_selectDifficultyComponent.TableView.ReloadData();
        
        _selectCharacteristicComponent.TableView.SelectCellWithIdx(0, true);
        //_selectDifficultyComponent.TableView.SelectCellWithIdx(_selectDifficultyComponent.NumberOfCells() - 1, true);

        detailsDescription.color = new Color(1f, 1f, 1f, 0.5f);
        detailsDescription.text = "Loading description...";
        Task.Run(async () =>
        {
            Plugin.DebugMessage("Description updated");
            detailsDescription.text = await FetchDescription(queuedSong.BsrKey);
            detailsDescription.color = Color.white;
        });
        
        UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
        {
            if (queuedSong.CoverImage == null)
            {
                detailsCoverImage.sprite = SongCore.Loader.defaultCoverImage;
                return;
            }
            
            detailsCoverImage.sprite = await Utilities.LoadSpriteAsync(queuedSong.CoverImage);
            Plugin.DebugMessage("Cover display updated");
        });
    }
    
    public void GoToLevel(BeatmapLevel? beatmapLevel)
    {
        Plugin.DebugMessage("GoToLevel called");

        if (beatmapLevel == null)
        {
            Plugin.DebugMessage("beatmapLevel is null");
            return;
        }
        
        _levelFilteringNavigationController.UpdateCustomSongs();
        
        _selectLevelCategoryViewController.LevelFilterCategoryIconSegmentedControlDidSelectCell(
            _selectLevelCategoryViewController._levelFilterCategoryIconSegmentedControl, 1);
        
        Plugin.DebugMessage($"Selecting {beatmapLevel.songName} in the map list...");
        _levelCollectionViewController._levelCollectionTableView.SelectLevel(beatmapLevel);
        Plugin.DebugMessage("Should be selected");
    }

    public void OkGoBack(NoncontextualizedSong queuedSong)
    {
        waitModal.Hide(false);
        
        Plugin.DebugMessage("Going back to the map list screen");
        GameObject.Find("QueueFlowCoordinator").GetComponent<QueueFlowCoordinator>().BackButtonWasPressed(this);
        try
        {
            GoToLevel(SongCore.Loader.GetLevelByHash(queuedSong.Hash));
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }

        Plugin.DebugMessage("Should've selected level");
    }

    [UIAction("skipButtonPressed")]
    public void SkipButtonPressed()
    {
        int index = _queueTableComponent.TableView._selectedCellIdxs.First();
        if (index == -1)
        {
            Plugin.DebugMessage("Nothing selected");
            return;
        }
        
        Plugin.DebugMessage($"Selected cell: {index}");
        
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
            Plugin.DebugMessage("Nothing selected");
            return;
        }
        
        waitModal.Show(false);
        
        Plugin.DebugMessage($"Selected cell: {index}");
        NoncontextualizedSong queuedSong = Queue[index];
        
        Plugin.DebugMessage($"Selected song: {queuedSong.Artist} - {queuedSong.Title} [{queuedSong.Mapper}]");
        
        Queue.RemoveAt(index);
        _queueTableComponent.TableView.ClearSelection();
        _queueTableComponent.TableView.ReloadData();
        
        ChatRequestButton.Instance.UseAttentiveButton(Queue.Count > 0);
        
        Beatmap? beatmap = await SongDetailsManager.BeatSaverInstance.Beatmap(queuedSong.BsrKey);
        if (beatmap != null)
        {
            Plugin.DebugMessage("Beatmap was not null");
            
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