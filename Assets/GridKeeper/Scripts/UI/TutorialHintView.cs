using GridKeeper.Tutorial;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridKeeper.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class TutorialHintView : MonoBehaviour
    {
        [SerializeField] private TutorialDirector tutorial;

        private VisualElement bubble;
        private Label text;

        private void Start()
        {
            if (tutorial == null) tutorial = FindAnyObjectByType<TutorialDirector>();
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            bubble = root.Q<VisualElement>("tutorial-hint");
            text = root.Q<Label>("tutorial-text");
            root.Q<Button>("tutorial-skip").clicked += tutorial.Skip;
            Button replay = root.Q<Button>("replay-tutorial-button");
            if (replay != null) replay.clicked += tutorial.Replay;

            tutorial.HintChanged += Render;
            Render(string.Empty, tutorial.IsActive);
        }

        private void OnDestroy()
        {
            if (tutorial != null) tutorial.HintChanged -= Render;
        }

        private void Render(string message, bool visible)
        {
            bubble.EnableInClassList("hidden", !visible || string.IsNullOrEmpty(message));
            text.text = message;
            bubble.RemoveFromClassList("tutorial-hint--pop");
            bubble.schedule.Execute(() => bubble.AddToClassList("tutorial-hint--pop")).StartingIn(20);
        }
    }
}
