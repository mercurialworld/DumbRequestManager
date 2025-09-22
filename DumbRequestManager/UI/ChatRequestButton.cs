﻿using System;
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
    [UIComponent("standardButton")] private Button _standardButton = null!;
    [UIComponent("attentionButton")] private Button _attentionButton = null!;
    // ReSharper restore FieldCanBeMadeReadOnly.Local
    
    private static readonly Color IdleColor = new Color(1, 1, 1, 0.5f);
    private static Color AttentionColor => 
        ColorUtility.TryParseHtmlString(Config.PrimaryColor, out var color)
            ? new Color(color.r - 0.1f, color.g - 0.1f, color.b - 0.1f, 2f)
            : new Color(203, 173, 255, 1f);
    
    public void Initialize()
    {
        Instance = this;
        
        bsmlParser.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(),
            "DumbRequestManager.UI.BSML.ChatRequestButton.bsml"),
            levelSelectionNavigationController.rectTransform.gameObject, this);
        
        _standardButton.gameObject.name = "DRM_StandardButton";
        _standardButton.transform.FindChildRecursively("Icon").GetComponent<ImageView>().color = IdleColor;

        _attentionButton.gameObject.name = "DRM_AttentionButton";
        _attentionButton.transform.FindChildRecursively("Icon").GetComponent<ImageView>().color = AttentionColor;
        
        if (_attentionButton.transform.Find("BG").TryGetComponent(out ImageView imageView))
        {
            imageView.material = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "AnimatedButton");
            imageView.SetAllDirty();
        }
        
        UseAttentiveButton(false);
    }

    public void Dispose()
    {
    }
    
    public void UseAttentiveButton(bool value)
    {
        _standardButton.gameObject.SetActive(!value);
        _attentionButton.gameObject.SetActive(value);
        
        _standardButton.transform.FindChildRecursively("Icon").GetComponent<ImageView>().color = value ? Color.white : IdleColor;
    }

    [UIAction("openQueue")]
    internal void OpenQueue()
    {
        mainFlowCoordinator.childFlowCoordinator.PresentFlowCoordinator(queueFlowCoordinator);
    }
}