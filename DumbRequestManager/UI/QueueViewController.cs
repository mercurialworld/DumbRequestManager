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
using DumbRequestManager.Services;
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
    
    // ReSharper disable FieldCanBeMadeReadOnly.Local
    [UIComponent("nothingSelectedPanel")]
    private VerticalLayoutGroup _nothingSelectedPanel = null!;
    [UIComponent("nothingSelectedFarPanel")]
    private VerticalLayoutGroup _nothingSelectedFarPanel = null!;
    [UIComponent("somethingSelectedPanel")]
    private VerticalLayoutGroup _somethingSelectedPanel = null!;
    [UIComponent("somethingSelectedFarPanel")]
    private VerticalLayoutGroup _somethingSelectedFarPanel = null!;
    
    [UIComponent("tagsChromaTag")]
    private HorizontalLayoutGroup _tagsChromaTag = null!;
    [UIComponent("tagsCinemaTag")]
    private HorizontalLayoutGroup _tagsCinemaTag = null!;
    [UIComponent("tagsMappingExtensionsTag")]
    private HorizontalLayoutGroup _tagsMappingExtensionsTag = null!;
    [UIComponent("tagsNoodleTag")]
    private HorizontalLayoutGroup _tagsNoodleTag = null!;
    [UIComponent("tagsVivifyTag")]
    private HorizontalLayoutGroup _tagsVivifyTag = null!;
    [UIComponent("tagsBeatLeaderRanked")]
    private HorizontalLayoutGroup _tagsBeatLeaderRankedTag = null!;
    [UIComponent("tagsScoreSaberRanked")]
    private HorizontalLayoutGroup _tagsScoreSaberRankedTag = null!;
    [UIComponent("tagsCurated")]
    private HorizontalLayoutGroup _tagsCuratedTag = null!;
    
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
    [UIComponent("detailsUpvotes")]
    public TextMeshProUGUI detailsUpvotes = null!;
    [UIComponent("detailsVotePercentage")]
    public TextMeshProUGUI detailsVotePercentage = null!;
    [UIComponent("detailsDownvotes")]
    public TextMeshProUGUI detailsDownvotes = null!;
    [UIComponent("detailsDescription")]
    public TextMeshProUGUI detailsDescription = null!;
    
    [UIComponent("detailsNotesPerSecond")]
    private static TextMeshProUGUI _detailsNps = null!;
    [UIComponent("detailsNoteJumpSpeed")]
    private static TextMeshProUGUI _detailsNjs = null!;
    // ReSharper restore FieldCanBeMadeReadOnly.Local
    
    [UIValue("difficultyChoices")]
    private static List<DifficultyUICellWrapper> _difficultyChoices = [];
    [UIValue("characteristicChoices")]
    private static List<CharacteristicUICellWrapper> _characteristicChoices = [];
    
    [UIValue("selectCharacteristicComponent")]
    private static CustomCellListTableData _selectCharacteristicComponent = null!;
    [UIValue("selectDifficultyComponent")]
    private static CustomCellListTableData _selectDifficultyComponent = null!;
    
    [UIComponent("confirmBanModal")]
    public ModalView confirmBanModal = null!;
    [UIComponent("banConfirmationText")]
    public TextMeshProUGUI banConfirmationText = null!;

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
            
            Material uiNoGlowRoundEdge = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "UINoGlowRoundEdge");
            detailsCoverImage.material = uiNoGlowRoundEdge;
            detailsDescription.lineSpacing = -25f;

            _tagsChromaTag.GetComponent<ImageView>().material = uiNoGlowRoundEdge;
            _tagsCinemaTag.GetComponent<ImageView>().material = uiNoGlowRoundEdge;
            _tagsMappingExtensionsTag.GetComponent<ImageView>().material = uiNoGlowRoundEdge;
            _tagsNoodleTag.GetComponent<ImageView>().material = uiNoGlowRoundEdge;
            _tagsVivifyTag.GetComponent<ImageView>().material = uiNoGlowRoundEdge;
            _tagsScoreSaberRankedTag.GetComponent<ImageView>().material = uiNoGlowRoundEdge;
            _tagsBeatLeaderRankedTag.GetComponent<ImageView>().material = uiNoGlowRoundEdge;
            _tagsCuratedTag.GetComponent<ImageView>().material = uiNoGlowRoundEdge;
        }
        else
        {
            _queueTableComponent.TableView.ClearSelection();
        }
        
        ToggleSelectionPanel(false);

        _queueTableComponent.TableView.ReloadDataKeepingPosition();
    }

    [UIAction("showBanModal")]
    [UsedImplicitly]
    private void ShowBanModal()
    {
        int idx = _queueTableComponent.TableView._selectedCellIdxs.First();
        NoncontextualizedSong queuedSong = Queue[idx];

        banConfirmationText.lineSpacing = -17;
        banConfirmationText.text = $"Are you sure you want to ban map <b>{queuedSong.BsrKey}</b>?";
        
        confirmBanModal.Show(true, true);
    }

    [UIAction("banSelectedMap")]
    [UsedImplicitly]
    private async Task BanSelectedMap()
    {
        int idx = _queueTableComponent.TableView._selectedCellIdxs.First();
        NoncontextualizedSong queuedSong = Queue[idx];
        
        _ = SkipButtonPressed(); // waiting on this doesn't matter
        
        SocketApi.Broadcast("pressedBan", queuedSong);
        await HookApi.TriggerHook("pressedBan", queuedSong);
        
        confirmBanModal.Hide(true);
    }

    [UIAction("hideBanModal")]
    [UsedImplicitly]
    private void HideBanModal()
    {
        confirmBanModal.Hide(true);
    }

    [UIAction("linkSelectedMap")]
    [UsedImplicitly]
    private async Task LinkSelectedMap()
    {
        int idx = _queueTableComponent.TableView._selectedCellIdxs.First();
        NoncontextualizedSong queuedSong = Queue[idx];
        
        SocketApi.Broadcast("pressedLink", queuedSong);
        await HookApi.TriggerHook("pressedLink", queuedSong);
    }
    
    [UIAction("pokeNextPerson")]
    [UsedImplicitly]
    private async Task PokeNextPerson()
    {
        int idx = _queueTableComponent.TableView._selectedCellIdxs.First();
        NoncontextualizedSong queuedSong = Queue[idx];
        
        SocketApi.Broadcast("pressedPoke", queuedSong);
        await HookApi.TriggerHook("pressedPoke", queuedSong);
    }

    private static readonly Color InactiveColor = new Color(1, 1, 1, 0.25f);
    private void SetMapModsTags(NoncontextualizedSong song)
    {
        _tagsChromaTag.GetComponent<ImageView>().color = song.UsesChroma ? Color.white : Color.black;
        _tagsChromaTag.GetComponentInChildren<TextMeshProUGUI>().color = song.UsesChroma ? Color.black : InactiveColor;
        
        _tagsCinemaTag.GetComponent<ImageView>().color = song.UsesCinema ? Color.white : Color.black;
        _tagsCinemaTag.GetComponentInChildren<TextMeshProUGUI>().color = song.UsesCinema ? Color.black : InactiveColor;
        
        _tagsMappingExtensionsTag.GetComponent<ImageView>().color = song.UsesMappingExtensions ? Color.white : Color.black;
        _tagsMappingExtensionsTag.GetComponentInChildren<TextMeshProUGUI>().color = song.UsesMappingExtensions ? Color.black : InactiveColor;
        
        _tagsNoodleTag.GetComponent<ImageView>().color = song.UsesNoodleExtensions ? Color.white : Color.black;
        _tagsNoodleTag.GetComponentInChildren<TextMeshProUGUI>().color = song.UsesNoodleExtensions ? Color.black : InactiveColor;
        
        _tagsVivifyTag.GetComponent<ImageView>().color = song.UsesVivify ? Color.white : Color.black;
        _tagsVivifyTag.GetComponentInChildren<TextMeshProUGUI>().color = song.UsesVivify ? Color.black : InactiveColor;
    }

    private static readonly Color BeatLeaderColor = new Color(1.0f, 0.0f, 0.4f, 1.0f);
    private static readonly Color ScoreSaberColor = new Color(1.0f, 0.867f, 0.102f, 1.0f);
    private static readonly Color CuratedColor = new Color(0f, 0.734f, 0.547f, 1.0f);
    private void SetMapRankedTags(NoncontextualizedSong song)
    {
        _tagsBeatLeaderRankedTag.GetComponent<ImageView>().color = song.BeatLeaderRanked ? BeatLeaderColor : Color.black;
        _tagsBeatLeaderRankedTag.GetComponentInChildren<TextMeshProUGUI>().color = song.BeatLeaderRanked ? Color.white : InactiveColor;
        
        _tagsScoreSaberRankedTag.GetComponent<ImageView>().color = song.ScoreSaberRanked ? ScoreSaberColor : Color.black;
        _tagsScoreSaberRankedTag.GetComponentInChildren<TextMeshProUGUI>().color = song.ScoreSaberRanked ? Color.black : InactiveColor;
        
        _tagsCuratedTag.GetComponent<ImageView>().color = song.Curated ? CuratedColor : Color.black;
        _tagsCuratedTag.GetComponentInChildren<TextMeshProUGUI>().color = song.Curated ? Color.white : InactiveColor;
    }
    
    // SDC doesn't cache Vivify yet, so i'm grabbing that data when we fetch the description
    // ...erm BeatSaverSharp doesn't, either. wuh oh
    //private void SetMapModsVivifyTag(bool hasVivify) => _tagsVivifyTag.gameObject.SetActive(hasVivify);

    private void ToggleSelectionPanel(bool value)
    {
        _somethingSelectedPanel.gameObject.SetActive(value);
        _somethingSelectedFarPanel.gameObject.SetActive(value);
        
        _nothingSelectedPanel.gameObject.SetActive(!value);
        _nothingSelectedFarPanel.gameObject.SetActive(!value);
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

    private static NoncontextualizedSong _selectedSong = null!;
    [UIAction("selectCell")]
    public void SelectCell(TableView tableView, NoncontextualizedSong queuedSong)
    {
        ToggleSelectionPanel(true);
        
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

        detailsUpvotes.text = queuedSong.Votes[0].ToString("N0");
        detailsDownvotes.text = queuedSong.Votes[1].ToString("N0");
        detailsVotePercentage.text = $"{(queuedSong.Rating * 100):N0}<size=80%><alpha=#AA>%";
        
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
        _selectCharacteristicComponent.TableView.ReloadData();
        _selectCharacteristicComponent.TableView.SelectCellWithIdx(0, true);

        detailsDescription.color = StandardColor;
        detailsDescription.text = "Loading description...";
        
        SetMapRankedTags(queuedSong);
        SetMapModsTags(queuedSong);

        Task.Run(async () =>
        {
            Beatmap? beatmap = await SongDetailsManager.BeatSaverInstance.Beatmap(queuedSong.BsrKey);

            if (beatmap != null)
            {
                Plugin.DebugMessage("Description updated");
                detailsDescription.text = beatmap.Description;
                detailsDescription.color = Color.white;
            }
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
        
        _levelFilteringNavigationController._levelSearchViewController.ResetFilter(false);
        _selectLevelCategoryViewController.LevelFilterCategoryIconSegmentedControlDidSelectCell(
                _selectLevelCategoryViewController._levelFilterCategoryIconSegmentedControl, 3);
        _levelFilteringNavigationController.HandleSelectLevelCategoryViewControllerDidSelectLevelCategory(
            _selectLevelCategoryViewController, SelectLevelCategoryViewController.LevelCategory.All);
        //_levelFilteringNavigationController.UpdateSecondChildControllerContent(SelectLevelCategoryViewController.LevelCategory.All);
        
        Plugin.DebugMessage($"Selecting {beatmapLevel.songName} in the map list...");
        _levelCollectionViewController._levelCollectionTableView.ReloadCellsData();
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
    private async Task SkipButtonPressed()
    {
        int index = _queueTableComponent.TableView._selectedCellIdxs.First();
        if (index == -1)
        {
            Plugin.DebugMessage("Nothing selected");
            return;
        }
        
        Plugin.DebugMessage($"Selected cell: {index}");
        
        NoncontextualizedSong queuedSong = Queue[index];
        Queue.RemoveAt(index);
        
        _queueTableComponent.TableView.ClearSelection();
        _queueTableComponent.TableView.ReloadData();
        
        ToggleSelectionPanel(false);
        
        ChatRequestButton.Instance.UseAttentiveButton(Queue.Count > 0);
        
        SocketApi.Broadcast("pressedSkip", queuedSong);
        await HookApi.TriggerHook("pressedSkip", queuedSong);
    }

    [UIAction("playButtonPressed")]
    public async Task PlayButtonPressed()
    {
        if (_loadingSpinner == null)
        {
            _loadingSpinner = Instantiate(Resources.FindObjectsOfTypeAll<LoadingControl>().First(), loadingSpinnerContainer.transform);
            
            Vector2 anchorMax = _loadingSpinner.GetComponent<RectTransform>().anchorMax;
            _loadingSpinner.GetComponent<RectTransform>().anchorMax = anchorMax with { y = 0.8667f };

            Transform? background = _loadingSpinner.transform.FindChildRecursively("DownloadingBG");
            if (background != null)
            {
                background.GetComponent<ImageView>().color = new Color(1f, 1f, 1f, 0.25f);
            }
            
            Transform? text = _loadingSpinner.transform.FindChildRecursively("DownloadingText");
            if (text != null)
            {
                text.GetComponent<TextMeshProUGUI>().richText = true;
            }
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
        
        if (!SongCore.Collections.songWithHashPresent(queuedSong.Hash))
        {
            Plugin.Log.Info("Beatmap doesn't exist locally, grabbing it");
            
            Beatmap? beatmap = await SongDetailsManager.BeatSaverInstance.Beatmap(queuedSong.BsrKey);
            if (beatmap != null)
            {
                Plugin.DebugMessage("Beatmap was not null");
                
                Progress<double> progress = new();
                progress.ProgressChanged += (_, value) =>
                {
                    _loadingSpinner.ShowDownloadingProgress($"Downloading map <color=#CBADFF><b>{queuedSong.BsrKey}</b> <color=#FFFFFF80>({(value * 100):0}%)", (float)value);
                };
                
                await SongDownloader.Instance.DownloadSong(beatmap, CancellationToken.None, progress);
                
                void LoaderOnSongsLoadedEvent(SongCore.Loader loader, ConcurrentDictionary<string, BeatmapLevel> concurrentDictionary)
                {
                    SongCore.Loader.SongsLoadedEvent -= LoaderOnSongsLoadedEvent;
                    OkGoBack(queuedSong);
                }
                
                SongCore.Loader.SongsLoadedEvent += LoaderOnSongsLoadedEvent;
                SongCore.Loader.Instance.RefreshSongs(false);
            }
        }
        else
        {
            OkGoBack(queuedSong);
        }
        
        SocketApi.Broadcast("pressedPlay", queuedSong);
        await HookApi.TriggerHook("pressedPlay", queuedSong);
    }
}