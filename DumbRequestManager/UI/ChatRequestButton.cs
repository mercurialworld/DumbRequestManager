using System;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using JetBrains.Annotations;
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
    public void Initialize()
    {
        bsmlParser.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(),
            "DumbRequestManager.UI.BSML.ChatRequestButton.bsml"),
            levelSelectionNavigationController.rectTransform.gameObject, this);
    }

    public void Dispose()
    {
    }

    [UIAction("openQueue")]
    internal void OpenQueue()
    {
        soloFreePlayFlowCoordinator.PresentFlowCoordinator(queueFlowCoordinator);
    }
}