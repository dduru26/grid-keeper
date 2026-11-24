using GridKeeper.AR;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridKeeper.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class PlacementPromptView : MonoBehaviour
    {
        [SerializeField] private ARPlacementController placement;
        [SerializeField] private string scanningText = "Move your phone slowly over a table";
        [SerializeField] private string readyText = "Tap to place Brightwater";

        private VisualElement prompt;
        private Label label;

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            prompt = root.Q<VisualElement>("placement-prompt");
            label = root.Q<Label>("prompt-text");

            Require.Reference(this, placement, nameof(placement));
            if (placement == null) return;

            placement.StateChanged += Render;
            Render(placement.State);
        }

        private void OnDisable()
        {
            if (placement != null) placement.StateChanged -= Render;
        }

        private void Render(PlacementState state)
        {
            if (prompt == null || label == null) return;

            prompt.style.display = state == PlacementState.Placed ? DisplayStyle.None : DisplayStyle.Flex;
            prompt.EnableInClassList("prompt--ready", state == PlacementState.ReadyToPlace);
            label.text = state == PlacementState.ReadyToPlace ? readyText : scanningText;
        }
    }
}
