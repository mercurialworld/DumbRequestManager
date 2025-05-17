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
using BeatSaverSharp.Models;
using BGLib.UnityExtension;
using DumbRequestManager.Classes;
using DumbRequestManager.Managers;
using DumbRequestManager.Services;
using DumbRequestManager.Utils;
using HMUI;
using IPA.Utilities.Async;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Zenject;
using PluginConfig = DumbRequestManager.Configuration.PluginConfig;

namespace DumbRequestManager.UI;

public readonly struct CharacteristicUICellWrapper(string name, string icon)
{
    [UIValue("icon")] public string Icon => icon;
    [UIValue("name")] public string Name => name;
}

public readonly struct DifficultyUICellWrapper(NoncontextualizedDifficulty difficulty)
{
    [UIValue("name")] public string Name => Normalize.GetDifficultyName(difficulty.Difficulty);
    public float NotesPerSecond => difficulty.NotesPerSecond;
    public float NoteJumpSpeed => difficulty.NoteJumpSpeed;
}

[ViewDefinition("DumbRequestManager.UI.BSML.QueueView.bsml")]
[HotReload(RelativePathToLayout = "BSML.QueueView.bsml")]
internal class QueueViewController : BSMLAutomaticViewController
{
    private LevelFilteringNavigationController _levelFilteringNavigationController = null!;
    private LevelCollectionViewController _levelCollectionViewController = null!;
    private LevelCollectionNavigationController _levelCollectionNavigationController = null!;
    private SelectLevelCategoryViewController _selectLevelCategoryViewController = null!;
    private SongPreviewPlayer _songPreviewPlayer = null!;
    private static DownloaderUtils _downloaderUtils = null!;
    private static BasicUIAudioManager _basicUIAudioManager = null!;
    
    private static LoadingControl _loadingSpinner = null!;

    private static PluginConfig Config => PluginConfig.Instance;
    internal static QueueViewController Instance = null!;
    
    [UIValue("queue")]
    private static List<NoncontextualizedSong> Queue => QueueManager.QueuedSongs;
    private static List<NoncontextualizedSong> MapsActedOn => QueueManager.MapsActedOn;
    
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
    internal static TableView? Table => _queueTableComponent.TableView;

    [UIComponent("waitModal")]
    private ModalView _waitModalActual = null!;
    private static ModalView WaitModal => Instance._waitModalActual; // sigh
    
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
    [UIComponent("detailsDescriptionScrollView")]
    public ScrollView detailsDescriptionScrollView = null!;
    
    [UIComponent("detailsNotesPerSecond")]
    private static TextMeshProUGUI _detailsNps = null!;
    [UIComponent("detailsNoteJumpSpeed")]
    private static TextMeshProUGUI _detailsNjs = null!;
    [UIComponent("detailsEstimatedStars")]
    private static TextMeshProUGUI _detailsEstimatedStars = null!;
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
    
    [UIComponent("tableSelector")]
    private static TabSelector _tableSelector = null!;
    
    [UIComponent("skipButton")]
    private static NoTransitionsButton _skipButton = null!;
    [UIComponent("reAddButton")]
    private static NoTransitionsButton _reAddButton = null!;

    private static int _activeTableTab;
    private static List<NoncontextualizedSong> ActiveList => _activeTableTab == 0 ? Queue : MapsActedOn;

    private static readonly Sprite BorderSprite = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "RoundRect10Border");

    [Inject]
    [UsedImplicitly]
    private void Construct(LevelFilteringNavigationController levelFilteringNavigationController,
        LevelCollectionViewController levelCollectionViewController,
        LevelCollectionNavigationController levelCollectionNavigationController,
        SelectLevelCategoryViewController selectLevelCategoryViewController,
        SongPreviewPlayer songPreviewPlayer,
        DownloaderUtils downloaderUtils)
    {
        _levelFilteringNavigationController = levelFilteringNavigationController;
        _levelCollectionViewController = levelCollectionViewController;
        _levelCollectionNavigationController = levelCollectionNavigationController;
        _selectLevelCategoryViewController = selectLevelCategoryViewController;
        _songPreviewPlayer = songPreviewPlayer;
        _downloaderUtils = downloaderUtils;
        
        Instance = this;
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
            _detailsEstimatedStars = GameObject.Find("DRM_DetailsEstimatedStars").GetComponent<TextMeshProUGUI>();
            _tableSelector = GameObject.Find("DRM_TableSelector").GetComponent<TabSelector>();
            _skipButton = GameObject.Find("DRM_SkipButton").GetComponent<NoTransitionsButton>();
            _reAddButton = GameObject.Find("DRM_ReAddButton").GetComponent<NoTransitionsButton>();
        }

        if (firstActivation)
        {
            _basicUIAudioManager = GameObject.Find("BasicUIAudioManager").GetComponent<BasicUIAudioManager>();
            
            _queueTableComponent.TableView.selectionType = TableViewSelectionType.Single;
            _queueTableComponent.TableView._spawnCellsThatAreNotVisible = true;
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

            detailsTitle.richText = true;

            _queueTableComponent.TableView.didDeselectCellWithIdxEvent += (_, _) =>
            {
                _songPreviewPlayer.CrossfadeToDefault();
                ClearHighlightedCells();
            };

            _detailsEstimatedStars.text = "<color=#FFCC55>\u2605 <color=#FFFFFF>-";
            
            _reAddButton.gameObject.SetActive(false);
        }
        else
        {
            _tableSelector.TextSegmentedControl.SelectCellWithNumber(0);
            ChangedTableView(_tableSelector.TextSegmentedControl, 0);
        }
        
        ToggleSelectionPanel(false);

        _queueTableComponent.TableView.ReloadDataKeepingPosition();
    }

    internal static void RefreshTableView()
    {
        if (_queueTableComponent == null)
        {
            return;
        }
        if (_queueTableComponent.TableView == null)
        {
            return;
        }

        int index;
        try
        {
            index = _queueTableComponent.TableView._selectedCellIdxs.First();
        }
        catch (Exception)
        {
            _queueTableComponent.TableView.RefreshCellsContent();
            YeetTableCells(_queueTableComponent.TableView);
            return;
        }
            
        NoncontextualizedSong selectedMap = ActiveList[index];
        _queueTableComponent.TableView.RefreshCellsContent();
        SetHighlightedCellsForUser(index, selectedMap);
        
        YeetTableCells(_queueTableComponent.TableView);
    }
    
    [UIAction("changedTableView")]
    private static void ChangedTableView(TextSegmentedControl _, int index)
    {
        Plugin.DebugMessage($"ChangedTableView: {index}");
        
        _skipButton.gameObject.SetActive(index == 0);
        _reAddButton.gameObject.SetActive(index == 1);

        _activeTableTab = index;
        
        _queueTableComponent.TableView.ClearSelection();
        Instance.ToggleSelectionPanel(false);

        UnityMainThreadTaskScheduler.Factory.StartNew(() =>
        {
            _queueTableComponent.Data = ActiveList;
            _queueTableComponent.TableView.ReloadData();
        });
        
        // wtf
        Instance._songPreviewPlayer.CrossfadeToDefault();
        
        YeetTableCells(_queueTableComponent.TableView);
    }

    [UIAction("showBanModal")]
    [UsedImplicitly]
    private void ShowBanModal()
    {
        int idx = _queueTableComponent.TableView._selectedCellIdxs.First();
        NoncontextualizedSong queuedSong = ActiveList[idx];

        banConfirmationText.lineSpacing = -17;
        banConfirmationText.text = $"Are you sure you want to ban map <b>{queuedSong.BsrKey}</b>?";
        
        confirmBanModal.Show(true, true);
    }

    [UIAction("banSelectedMap")]
    [UsedImplicitly]
    private async Task BanSelectedMap()
    {
        int idx = _queueTableComponent.TableView._selectedCellIdxs.First();
        NoncontextualizedSong queuedSong = ActiveList[idx];

        if (_activeTableTab == 0)
        {
            _ = SkipButtonPressed(); // waiting on this doesn't matter
        }

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
        NoncontextualizedSong queuedSong = ActiveList[idx];
        
        SocketApi.Broadcast("pressedLink", queuedSong);
        await HookApi.TriggerHook("pressedLink", queuedSong);
    }
    
    [UIAction("pokeNextPerson")]
    [UsedImplicitly]
    private async Task PokeNextPerson()
    {
        int idx = _queueTableComponent.TableView._selectedCellIdxs.First();
        NoncontextualizedSong queuedSong = ActiveList[idx];
        
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

    internal void ToggleSelectionPanel(bool value)
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
        _basicUIAudioManager.HandleButtonClickEvent();
        
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

        UnityMainThreadTaskScheduler.Factory.StartNew(() =>
        {
            _selectDifficultyComponent.Data = _difficultyChoices;

            _selectDifficultyComponent.TableView.ReloadData();
            Plugin.DebugMessage("Reloaded characteristics/difficulties UI");

            _selectDifficultyComponent.TableView.SelectCellWithIdx(_selectDifficultyComponent.NumberOfCells() - 1, true);
            Plugin.DebugMessage("Should have selected difficulty");
        });
    }
    
    private static void DidSelectDifficultyCellWithIdxEvent(TableView tableView, int idx)
    {
        _basicUIAudioManager.HandleButtonClickEvent();
        
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

        NoncontextualizedSong queuedSong = ActiveList[_queueTableComponent.TableView._selectedCellIdxs.First()];
        if (queuedSong.IsWip)
        {
            _detailsEstimatedStars.text = "<size=95%><color=#FFCC55>\u2605</size> <color=#FFFFFF>-";
        }
        else
        {
            UpdateStarDisplay(difficulty);
        }
    }

    private static void UpdateStarDisplay(DifficultyUICellWrapper? difficulty = null)
    {
        if (_starsList == null)
        {
            Plugin.DebugMessage("_starsList is null");
            _detailsEstimatedStars.text = "<size=95%><color=#FFCC55>\u2605</size> <color=#FFFFFF>-";
            return;
        }
        
        string diffName = difficulty == null ? _difficultyChoices[_selectDifficultyComponent.TableView._selectedCellIdxs.First()].Name : difficulty.Value.Name;
            
        BeatLeaderDifficulty starsObject = _starsList.Find(x =>
            x.CharacteristicName ==
            _characteristicChoices[_selectCharacteristicComponent.TableView._selectedCellIdxs.First()].Name &&
            x.DifficultyName == diffName);
                
        _detailsEstimatedStars.text = $"<size=95%><color=#FFCC55>\u2605</size> <color=#FFFFFF>{starsObject.Stars:0.00}";
    }

    private static void SetHighlightedCellsForUser(int ignoreIndex, NoncontextualizedSong queuedSong)
    {
        TableView tableView = _queueTableComponent.TableView;
        
        for (int idx = 0; idx < ActiveList.Count; idx++)
        {
            TableCell? tableCell = tableView.GetCellAtIndex(idx);
            Transform? cellTransform = tableCell?.transform.FindChildRecursively("CellHighlightBG");

            if (cellTransform == null)
            {
                continue;
            }
            
            cellTransform.GetComponent<ImageView>().sprite = BorderSprite;
            cellTransform.GetComponent<ImageView>().overrideSprite = BorderSprite;
            
            cellTransform.gameObject.SetActive(queuedSong.User == ActiveList[idx].User && ignoreIndex != idx);
        }
    }

    private static void ClearHighlightedCells()
    {
        TableView tableView = _queueTableComponent.TableView;
        
        for (int idx = 0; idx < ActiveList.Count; idx++)
        {
            TableCell? tableCell = tableView.GetCellAtIndex(idx);
            Transform? cellTransform = tableCell?.transform.FindChildRecursively("CellHighlightBG");
            cellTransform?.gameObject.SetActive(false);
        }
    }

    private static void YeetTableCells(TableView tableView)
    {
        // (i like my code readable)
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (KeyValuePair<string, List<TableCell>> pair in tableView._reusableCells)
        {
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (TableCell? cell in pair.Value)
            {
                if (cell == null)
                {
                    continue;
                }
                
                if (!cell.gameObject.activeSelf)
                {
                    Destroy(cell.gameObject);
                }
            }
        } 
    }

    private static NoncontextualizedSong _selectedSong = null!;
    private static UnityWebRequest? _webRequest;
    private static CancellationTokenSource _descriptionCancellationToken = new();
    private static CancellationTokenSource _starEstimateCancellationToken = new();
    private static List<BeatLeaderDifficulty>? _starsList;
    [UIAction("selectCell")]
    public void SelectCell(TableView tableView, NoncontextualizedSong queuedSong)
    {
        ToggleSelectionPanel(true);
        ClearHighlightedCells();
        _basicUIAudioManager.HandleButtonClickEvent();
        YeetTableCells(_queueTableComponent.TableView);
        _starsList = null;
        
        try
        {
            if (_descriptionCancellationToken.Token.CanBeCanceled)
            {
                _descriptionCancellationToken.Cancel();
                _descriptionCancellationToken.Dispose();
            }
        }
        catch (Exception exception)
        {
            if (exception is ObjectDisposedException)
            {
                Plugin.DebugMessage("Token is already disposed");
            }
        }
        
        _selectedSong = queuedSong;
        int index = tableView._selectedCellIdxs.First();

        if (!string.IsNullOrEmpty(queuedSong.User))
        {
            SetHighlightedCellsForUser(index, queuedSong);
        }

        Plugin.DebugMessage($"Selected cell: {index}");
        Plugin.DebugMessage($"Selected song: {queuedSong.Artist} - {queuedSong.Title} [{queuedSong.Mapper}]");
        Plugin.DebugMessage($"Cells: {tableView._contentTransform.childCount}");

        detailsTitle.text = queuedSong.SubTitle == string.Empty ? queuedSong.Title : $"{queuedSong.Title} <size=75%><alpha=#AA>{queuedSong.SubTitle}";
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
        _characteristicChoices = characteristics.Select(x => new CharacteristicUICellWrapper(x, Normalize.GetCharacteristicIcon(x))).ToList();
        Plugin.DebugMessage("Updated characteristic choices");
        _difficultyChoices = queuedSong.Diffs.Where(x => x.Characteristic == _characteristicChoices[0].Name).Select(x => new DifficultyUICellWrapper(x)).ToList();
        Plugin.DebugMessage($"Got {_difficultyChoices.Count} unique difficulties");
        
        UnityMainThreadTaskScheduler.Factory.StartNew(() =>
        {
            _selectCharacteristicComponent.Data = _characteristicChoices;
            _selectCharacteristicComponent.TableView.ReloadData();
            _selectCharacteristicComponent.TableView.SelectCellWithIdx(0, true);
        });

        // this fixes stuff not centering on the first selected map in the session
        // cursed, i know. this is why it's not aligned to the center in the BSML file
        if (!_selectCharacteristicComponent.TableView._alignToCenter)
        {
            UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
            {
                await Task.Delay(1);
                _selectCharacteristicComponent.TableView._alignToCenter = true;
                _selectCharacteristicComponent.TableView.RefreshCellsContent();
            });
        }
        
        YeetTableCells(_selectCharacteristicComponent.TableView);
        YeetTableCells(_selectDifficultyComponent.TableView);

        detailsDescriptionScrollView.ScrollTo(0, false);
        detailsDescription.color = StandardColor;
        detailsDescription.text = "Loading description...";
        
        SetMapRankedTags(queuedSong);
        SetMapModsTags(queuedSong);

        Task.Run(async () =>
        {
            if (queuedSong.IsWip)
            {
                detailsDescription.text = "Map is a WIP, no description available.";
                detailsDescription.color = Color.white;
                return;
            }
            
            _descriptionCancellationToken = new CancellationTokenSource();
            
            Plugin.DebugMessage("(downloading BeatSaver data)");
            Beatmap? beatmap = await SongDetailsManager.BeatSaverInstance.Beatmap(queuedSong.BsrKey, _descriptionCancellationToken.Token);

            if (!_descriptionCancellationToken.IsCancellationRequested)
            {
                if (beatmap != null)
                {
                    Plugin.DebugMessage("Description updated");
                    detailsDescription.text = beatmap.Description;
                    detailsDescription.color = Color.white;
                }
            }
            else
            {
                Plugin.DebugMessage("Description data cancelled");
            }
            
            _descriptionCancellationToken.Dispose();
        });
        
        Task.Run(async () =>
        {
            if (queuedSong.IsWip)
            {
                _detailsEstimatedStars.text = "<size=95%><color=#FFCC55>\u2605</size> <color=#FFFFFF>-";
                return;
            }
            
            _starEstimateCancellationToken = new CancellationTokenSource();
            
            Plugin.DebugMessage("(downloading BeatLeader data)");
            _starsList = await BeatLeaderUtils.GetStarValueForHash(queuedSong.Hash, _starEstimateCancellationToken.Token);

            UpdateStarDisplay();
        });
        
        UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
        {
            if (queuedSong.CoverImage == null)
            {
                detailsCoverImage.sprite = queuedSong.CoverImageSprite;
                return;
            }
            
            detailsCoverImage.sprite = await Utilities.LoadSpriteAsync(queuedSong.CoverImage);
            Plugin.DebugMessage("Cover display updated");
        });

        if (!Config.PlayAudioPreviews || queuedSong.IsWip)
        {
            return;
        }
        
        _songPreviewPlayer._audioManager._audioMixer.GetFloat("MusicVolume", out float volume);
        BeatmapLevel? localLevel = SongCore.Loader.GetLevelByHash(queuedSong.Hash);
        if (localLevel != null)
        {
            UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
            {
                AudioClip previewAudioClip = await localLevel.previewMediaData.GetPreviewAudioClip();
                _songPreviewPlayer.CrossfadeTo(previewAudioClip, volume, localLevel.previewStartTime, localLevel.previewDuration, false, null);
            });
        }
        else
        {
            if (!Config.PlayRemoteAudioPreviews)
            {
                _songPreviewPlayer.CrossfadeToDefault();
                return;
            }
            
            UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
            {
                Beatmap? remoteData = await SongDetailsManager.BeatSaverInstance.Beatmap(queuedSong.BsrKey);
                if (remoteData != null)
                {
                    if (_webRequest != null)
                    {
                        // ReSharper disable once MergeIntoPattern
                        if (!_webRequest.isDone)
                        {
                            _webRequest.Abort();
                        }
                    }

                    _webRequest = UnityWebRequestMultimedia.GetAudioClip(remoteData.LatestVersion.PreviewURL, AudioType.MPEG);
                    await _webRequest.SendWebRequestAsync();
                    
                    if (_webRequest.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
                    {
                        _songPreviewPlayer.CrossfadeToDefault();
                    }
                    else
                    {
                        if (_webRequest.isDone)
                        {
                            AudioClip previewAudioClip = DownloadHandlerAudioClip.GetContent(_webRequest);
                            _songPreviewPlayer.CrossfadeTo(previewAudioClip, volume, 0, previewAudioClip.length, false, null);
                        }   
                    }
                }
                else
                {
                    _songPreviewPlayer.CrossfadeToDefault();
                }
            });
        }
    }
    
    protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
    {
        _songPreviewPlayer.CrossfadeToDefault();
        base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
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

        UnityMainThreadTaskScheduler.Factory.StartNew(() =>
        {
            Plugin.DebugMessage($"Selecting {beatmapLevel.songName} in the map list...");
            _levelCollectionViewController._levelCollectionTableView.ReloadCellsData();

            if (!beatmapLevel.levelID.Contains("WIP"))
            {
                _levelCollectionViewController._levelCollectionTableView.SelectLevel(beatmapLevel);
            }
            else if (SongCore.Loader.CustomLevelsRepository != null)
            {
                // used wipbot for reference. no idea why the above won't work for WIPs
                // https://github.com/Danielduel/wipbot/blob/961d70157f21046997c7466621a68dcf72527e6e/wipbot/Plugin.cs#L477
                foreach (BeatmapLevelPack levelPack in SongCore.Loader.CustomLevelsRepository.beatmapLevelPacks)
                {
                    foreach (BeatmapLevel level in levelPack.AllBeatmapLevels().Where(level =>
                                 level.levelID.StartsWith("custom_level_" + beatmapLevel.levelID.Split('_')[2])))
                    {
                        _levelCollectionNavigationController.SelectLevel(level);
                        break;
                    }
                }
            }

            Plugin.DebugMessage("Should be selected");
        });
    }

    public void OkGoBack(NoncontextualizedSong queuedSong, BeatmapLevel? beatmapLevel = null)
    {
        WaitModal.Hide(false);
        
        Plugin.DebugMessage("Going back to the map list screen");
        try
        {
            GameObject.Find("QueueFlowCoordinator").GetComponent<QueueFlowCoordinator>().BackButtonWasPressed(this);
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }

        try
        {
            if (beatmapLevel != null)
            {
                GoToLevel(beatmapLevel);
            }
            else
            {
                Plugin.DebugMessage($"Using {queuedSong.Hash} for GoToLevel within OkGoBack");
                GoToLevel(SongCore.Loader.GetLevelByHash(queuedSong.Hash));
            }
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
        
#if !DEBUG
        QueueManager.Save();
#endif
        
        _queueTableComponent.TableView.ClearSelection();
        _queueTableComponent.TableView.ReloadData();
        
        ToggleSelectionPanel(false);
        
        ChatRequestButton.Instance.UseAttentiveButton(Queue.Count > 0);
        
        MapsActedOn.Insert(0, queuedSong);
        
        _songPreviewPlayer.CrossfadeToDefault();
        
        SocketApi.Broadcast("pressedSkip", queuedSong);
        await HookApi.TriggerHook("pressedSkip", queuedSong);
    }
    
    [UIAction("reAddButtonPressed")]
    [UsedImplicitly]
    private void ReAddButtonPressed()
    {
        int index = _queueTableComponent.TableView._selectedCellIdxs.First();
        if (index == -1)
        {
            Plugin.DebugMessage("Nothing selected");
            return;
        }
        
        Plugin.DebugMessage($"Selected cell: {index}");
        
        NoncontextualizedSong selectedMap = MapsActedOn[index];
        MapsActedOn.RemoveAt(index);
        Queue.Insert(0, selectedMap);
        
#if !DEBUG
        QueueManager.Save();
#endif
        
        _queueTableComponent.TableView.ClearSelection();
        _queueTableComponent.TableView.ReloadData();
        
        ToggleSelectionPanel(false);
        
        _songPreviewPlayer.CrossfadeToDefault();
        
        SocketApi.Broadcast("mapReAdded", selectedMap);
        _ = HookApi.TriggerHook("mapReAdded", selectedMap);
        
        ChatRequestButton.Instance.UseAttentiveButton(Queue.Count > 0);
    }

    private class DownloadSongHandler(string bsrKey, Beatmap? beatmap, NoncontextualizedSong queuedSong)
    {
        internal readonly CancellationTokenSource TokenSource = new ();
        
        public async Task Start()
        {
            Progress<float> progress = new();
            progress.ProgressChanged += (_, value) =>
            {
                _loadingSpinner.ShowDownloadingProgress($"Downloading {(queuedSong.IsWip ? "WIP map" : "map")} <color=#CBADFF><b>{bsrKey}</b> <color=#FFFFFF80><size=80%>(<mspace=0.41em>{(value * 100):0}</mspace>%)", value);
            };

            try
            {
                if (queuedSong.IsWip)
                {
                    await _downloaderUtils.DownloadWip(queuedSong, TokenSource.Token, progress);
                }
                else
                {
                    await _downloaderUtils.DownloadUsingKey(beatmap!, TokenSource.Token, progress);   
                }
            }
            catch (Exception exception)
            {
                if (exception is TaskCanceledException)
                {
                    Plugin.DebugMessage("exception is TaskCanceledException");
                
                    WaitModal.Hide(false);
                    return;
                }

                Plugin.Log.Error(exception);
            }
            
            SongCore.Loader.SongsLoadedEvent += LoaderOnSongsLoadedEvent;

            if (TokenSource.IsCancellationRequested)
            {
                Plugin.DebugMessage("TokenSource.IsCancellationRequested");
                
                WaitModal.Hide(false);
                return;
            }
            
            if (!queuedSong.IsWip)
            {
                // refreshing on wips needs to be handled in DownloadWip, not here
                SongCore.Loader.Instance.RefreshSongs(false);
            }

            return;

            void LoaderOnSongsLoadedEvent(SongCore.Loader loader, ConcurrentDictionary<string, BeatmapLevel> concurrentDictionary)
            {
                SongCore.Loader.SongsLoadedEvent -= LoaderOnSongsLoadedEvent;
                if (!queuedSong.IsWip)
                {
                    Instance.OkGoBack(queuedSong);
                }
            }
        }

        public void Cancel()
        {
            Plugin.Log.Info("Download cancelled");
            TokenSource.Cancel();
            TokenSource.Dispose();
        }
    }

    private static DownloadSongHandler? _downloadHandler;

    [UIAction("cancelDownload")]
    public void CancelDownload()
    {
        Plugin.DebugMessage("Wants to cancel map download");
        _downloadHandler?.Cancel();
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
        
        WaitModal.Show(false);
        
        Plugin.DebugMessage($"Selected cell: {index}");
        NoncontextualizedSong queuedSong = ActiveList[index];
        
        Plugin.DebugMessage($"Selected song: {queuedSong.Artist} - {queuedSong.Title} [{queuedSong.Mapper}]");

        if (queuedSong.IsWip)
        {
            if (_downloadHandler != null)
            {
                // ReSharper disable once MergeIntoPattern
                if (!_downloadHandler.TokenSource.IsCancellationRequested)
                {
                    _downloadHandler.Cancel();
                }
            }

            _downloadHandler = new DownloadSongHandler(queuedSong.BsrKey, null, queuedSong);
            await _downloadHandler.Start();

            if (_downloadHandler.TokenSource.IsCancellationRequested)
            {
                return;
            }
        }
        else if (!SongCore.Collections.songWithHashPresent(queuedSong.Hash))
        {
            Plugin.Log.Info("Beatmap doesn't exist locally, grabbing it");
            
            Beatmap? beatmap = await SongDetailsManager.BeatSaverInstance.Beatmap(queuedSong.BsrKey);
            if (beatmap != null)
            {
                Plugin.DebugMessage("Beatmap was not null");

                if (_downloadHandler != null)
                {
                    // ReSharper disable once MergeIntoPattern
                    if (!_downloadHandler.TokenSource.IsCancellationRequested)
                    {
                        _downloadHandler.Cancel();
                    }
                }

                _downloadHandler = new DownloadSongHandler(queuedSong.BsrKey, beatmap, queuedSong);
                await _downloadHandler.Start();

                if (_downloadHandler.TokenSource.IsCancellationRequested)
                {
                    return;
                }
            }
        }
        else
        {
            OkGoBack(queuedSong);
        }

        if (_activeTableTab == 0)
        {
            await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                Queue.RemoveAt(index);
                _queueTableComponent.TableView.ClearSelection();
                _queueTableComponent.TableView.ReloadData();
            });

            MapsActedOn.Insert(0, queuedSong);

            ChatRequestButton.Instance.UseAttentiveButton(Queue.Count > 0);
        }

        // persistence saving is taken care of in the downloader
        // i don't want it triggering unless everything goes to plan
        
        SocketApi.Broadcast("pressedPlay", queuedSong);
        await HookApi.TriggerHook("pressedPlay", queuedSong);
    }

    public static void RefreshQueue()
    {
        if (_queueTableComponent != null)
        {
            _ = UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                _queueTableComponent.TableView.ReloadData();
            });
        }
    }
}