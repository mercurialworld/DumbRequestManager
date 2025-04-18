using System;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
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
    SoloFreePlayFlowCoordinator soloFreePlayFlowCoordinator,
    QueueFlowCoordinator queueFlowCoordinator)
    : IInitializable, IDisposable
{
    public static ChatRequestButton Instance = null!;
    
    // ReSharper disable FieldCanBeMadeReadOnly.Local
    [UIComponent("standardButton")] private Button _standardButton = null!;
    [UIComponent("attentionButton")] private Button _attentionButton = null!;
    // ReSharper restore FieldCanBeMadeReadOnly.Local
    
    public void Initialize()
    {
        Instance = this;
        
        bsmlParser.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(),
            "DumbRequestManager.UI.BSML.ChatRequestButton.bsml"),
            levelSelectionNavigationController.rectTransform.gameObject, this);
        
        _standardButton.gameObject.name = "DRM_StandardButton";
        _attentionButton.gameObject.name = "DRM_AttentionButton";

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
    }

    [UIAction("openQueue")]
    internal void OpenQueue()
    {
        soloFreePlayFlowCoordinator.PresentFlowCoordinator(queueFlowCoordinator);
    }
}