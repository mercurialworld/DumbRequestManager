using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using DumbRequestManager.Configuration;
using DumbRequestManager.Managers;
using DumbRequestManager.Services;
using HMUI;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace DumbRequestManager.UI;

[ViewDefinition("DumbRequestManager.UI.BSML.SideSettings.bsml")]
[HotReload(RelativePathToLayout = "BSML.SideSettings.bsml")]
internal class SideSettingsViewController : BSMLAutomaticViewController
{
    private static PluginConfig Config => PluginConfig.Instance;
    
    [UIValue("ShowRequestersInsteadOfMappers")]
    public bool ShowRequestersInsteadOfMappers {
        get => Config.ShowRequestersInsteadOfMappers;
        set
        {
            Config.ShowRequestersInsteadOfMappers = value;
            QueueViewController.RefreshTableView();
        }
    }
    
    [UIValue("IsQueueOpen")]
    public bool IsQueueOpen { get; set; }
    public static SideSettingsViewController Instance { get; private set; } = null!;
    
    private readonly VersionManager _versionData = new VersionManager();

    // ReSharper disable FieldCanBeMadeReadOnly.Local
    // ReSharper disable FieldCanBeMadeReadOnly.Global
    [UIComponent("queueOpenToggle")]
    private ToggleSetting? _toggleSettingObject = null!;
    [UIComponent("updateObject")]
    internal HorizontalLayoutGroup UpdateObject = null!;
    [UIComponent("confirmClearModal")]
    public ModalView confirmClearModal = null!;
    [UIComponent("confirmShuffleModal")]
    public ModalView confirmShuffleModal = null!;
    // ReSharper restore FieldCanBeMadeReadOnly.Global
    // ReSharper restore FieldCanBeMadeReadOnly.Local
    
    [UIValue("updateString")]
    [UsedImplicitly]
    private string UpdateString => $"(<alpha=#CC>{_versionData.ModVersion.ToString(3)} <alpha=#88>-> <alpha=#CC>{VersionManager.LatestVersion?.ToString(3)}<alpha=#FF>)";

    internal SideSettingsViewController()
    {
        Instance = this;
    }
    
    [UIAction("openNewReleaseTag")]
    [UsedImplicitly]
    private void OpenNewReleaseTag()
    {
        Application.OpenURL($"https://github.com/mercurialworld/DumbRequestManager/releases/tag/{VersionManager.LatestVersion?.ToString(3)}");
    }
    

    [UIAction("setState")]
    public async Task SetState(bool isQueueOpen)
    {
        if (isQueueOpen == IsQueueOpen)
        {
            return;
        }
        
        IsQueueOpen = isQueueOpen;
        NotifyPropertyChanged(nameof(IsQueueOpen));
        
        if (_toggleSettingObject != null)
        {
            if (_toggleSettingObject.isActiveAndEnabled)
            {
                _toggleSettingObject.Value = IsQueueOpen;
            }
        }
        
        SocketApi.Broadcast("queueOpen", isQueueOpen);
        await HookApi.TriggerHook("queueOpen", isQueueOpen);
    }

    [UIAction("clearQueue")]
    public void ClearQueue()
    {
        QueueManager.QueuedSongs.Clear();
                            
        QueueViewController.RefreshQueue();
        ChatRequestButton.Instance.UseAttentiveButton(false);
        
        confirmClearModal.Hide(true);
        
        QueueViewController.Instance.ToggleSelectionPanel(false);
        
        SocketApi.Broadcast("queueCleared");
        _ = HookApi.TriggerHook("queueCleared");
    }
    
    [UIAction("showClearModal")]
    [UsedImplicitly]
    private void ShowClearModal()
    {
        confirmClearModal.Show(true, true);
    }
    
    [UIAction("hideClearModal")]
    [UsedImplicitly]
    private void HideClearModal()
    {
        confirmClearModal.Hide(true);
    }
    
    [UIAction("shuffleQueue")]
    public void ShuffleQueue()
    {
        QueueManager.Shuffle();
        confirmShuffleModal.Hide(true);
    }
    
    [UIAction("showShuffleModal")]
    [UsedImplicitly]
    private void ShowShuffleModal()
    {
        confirmShuffleModal.Show(true, true);
    }
    
    [UIAction("hideShuffleModal")]
    [UsedImplicitly]
    private void HideShuffleModal()
    {
        confirmShuffleModal.Hide(true);
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        
        if (_toggleSettingObject != null)
        {
            _toggleSettingObject.Value = IsQueueOpen;
        }
        
#if !DEBUG
        UpdateObject.gameObject.SetActive(VersionManager.LatestVersion > _versionData.ModVersion);
#endif
    }
}