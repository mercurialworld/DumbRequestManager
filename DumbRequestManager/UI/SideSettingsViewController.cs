using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using DumbRequestManager.Services;
using UnityEngine.UIElements;

namespace DumbRequestManager.UI;

[ViewDefinition("DumbRequestManager.UI.BSML.SideSettings.bsml")]
[HotReload(RelativePathToLayout = "BSML.SideSettings.bsml")]
internal class SideSettingsViewController : BSMLAutomaticViewController
{
    [UIValue("IsQueueOpen")] public bool IsQueueOpen { get; set; }
    public static SideSettingsViewController Instance { get; private set; } = null!;

    internal SideSettingsViewController()
    {
        Instance = this;
    }

    // ReSharper disable FieldCanBeMadeReadOnly.Local
    [UIComponent("queueOpenToggle")]
    private Toggle _toggleSettingObject = null!;
    // ReSharper restore FieldCanBeMadeReadOnly.Local

    [UIAction("setState")]
    public async Task SetState(bool isQueueOpen)
    {
        IsQueueOpen = isQueueOpen;
        NotifyPropertyChanged(nameof(IsQueueOpen));
        
        SocketApi.Broadcast("queueOpen", isQueueOpen);
        await HookApi.TriggerHook("queueOpen", isQueueOpen);
        
        _toggleSettingObject.value = isQueueOpen;
    }
}