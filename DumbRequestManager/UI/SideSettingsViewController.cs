using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using DumbRequestManager.Managers;
using DumbRequestManager.Services;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace DumbRequestManager.UI;

[ViewDefinition("DumbRequestManager.UI.BSML.SideSettings.bsml")]
[HotReload(RelativePathToLayout = "BSML.SideSettings.bsml")]
internal class SideSettingsViewController : BSMLAutomaticViewController
{
    [UIValue("IsQueueOpen")]
    public bool IsQueueOpen { get; set; }
    public static SideSettingsViewController Instance { get; private set; } = null!;
    
    private readonly VersionManager _versionData = new VersionManager();

    // ReSharper disable FieldCanBeMadeReadOnly.Local
    // ReSharper disable FieldCanBeMadeReadOnly.Global
    [UIComponent("queueOpenToggle")]
    private ToggleSetting _toggleSettingObject = null!;
    [UIComponent("updateObject")]
    internal HorizontalLayoutGroup UpdateObject = null!;
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
        Application.OpenURL($"https://github.com/TheBlackParrot/DumbRequestManager/releases/tag/{VersionManager.LatestVersion?.ToString(3)}");
    }
    

    [UIAction("setState")]
    public async Task SetState(bool isQueueOpen)
    {
        IsQueueOpen = isQueueOpen;
        NotifyPropertyChanged(nameof(IsQueueOpen));
        
        SocketApi.Broadcast("queueOpen", isQueueOpen);
        await HookApi.TriggerHook("queueOpen", isQueueOpen);
        
        _toggleSettingObject.Value = IsQueueOpen;
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        UpdateObject.gameObject.SetActive(VersionManager.LatestVersion > _versionData.ModVersion);
    }
}