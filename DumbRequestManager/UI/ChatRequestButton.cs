using System;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using DumbRequestManager.Configuration;
using HMUI;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace DumbRequestManager.UI;

[UsedImplicitly]
internal class ChatRequestButton(
    LevelSelectionNavigationController levelSelectionNavigationController,
    BSMLParser bsmlParser,
    MainFlowCoordinator mainFlowCoordinator,
    QueueFlowCoordinator queueFlowCoordinator)
    : IInitializable, IDisposable
{
    private static PluginConfig Config => PluginConfig.Instance;
    public static ChatRequestButton Instance = null!;
    
    // ReSharper disable FieldCanBeMadeReadOnly.Local
    [UIComponent("requestButton")] private Button _requestButton = null!;
    // ReSharper restore FieldCanBeMadeReadOnly.Local
    
    private static readonly Color IdleColor = new Color(1, 1, 1, 0.5f);
    private static Color AttentionColor => 
        ColorUtility.TryParseHtmlString(Config.AttentionColor, out var color)
            ? color
            : new Color(255, 114, 118, 1f);
    
    public void Initialize()
    {
        Instance = this;
        
        bsmlParser.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(),
            "DumbRequestManager.UI.BSML.ChatRequestButton.bsml"),
            levelSelectionNavigationController.rectTransform.gameObject, this);
        
        _requestButton.gameObject.name = "DRM_RequestButton";
        _requestButton.transform.FindChildRecursively("Icon").GetComponent<ImageView>().color = IdleColor;
        
        UseAttentiveButton(false);
    }

    public void Dispose()
    {
    }

    private void MakeButtonAttentive()
    {
        _requestButton.transform.FindChildRecursively("Icon").GetComponent<ImageView>().color = AttentionColor;
        
        if (!_requestButton.transform.Find("BG").TryGetComponent(out ImageView imageView)) return;
        imageView.material = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "AnimatedButton");
        imageView.SetAllDirty();
    }
    
    private void MakeButtonStandard()
    {
        _requestButton.transform.FindChildRecursively("Icon").GetComponent<ImageView>().color = IdleColor;
        
        if (!_requestButton.transform.Find("BG").TryGetComponent(out ImageView imageView)) return;
        imageView.material = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "UINoGlow");
        imageView.SetAllDirty();
    }
    
    public void UseAttentiveButton(bool attention)
    {
        if (attention)
        {
            MakeButtonAttentive();
        }
        else
        {
            MakeButtonStandard();
        }
    }

    [UIAction("openQueue")]
    internal void OpenQueue()
    {
        mainFlowCoordinator.childFlowCoordinator.PresentFlowCoordinator(queueFlowCoordinator);
    }
}