using System.Collections.Generic;
using GridKeeper.AR;
using GridKeeper.Construction;
using GridKeeper.Core;
using GridKeeper.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridKeeper.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class BuildTrayView : MonoBehaviour
    {
        [SerializeField] private BuildToolbar toolbar;
        [SerializeField] private GameManager game;
        [SerializeField] private ARPlacementController placement;

        private readonly Dictionary<SourceKind, Button> buttons = new Dictionary<SourceKind, Button>();
        private readonly Dictionary<SourceKind, PlaceableDefinition> definitions = new Dictionary<SourceKind, PlaceableDefinition>();
        private VisualElement layer;
        private Button removeButton;
        private Label hint;

        private void Start()
        {
            if (toolbar == null) toolbar = FindAnyObjectByType<BuildToolbar>();
            if (game == null) game = FindAnyObjectByType<GameManager>();
            if (placement == null) placement = FindAnyObjectByType<ARPlacementController>();

            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            layer = root.Q<VisualElement>("build-layer");
            hint = root.Q<Label>("build-hint");
            removeButton = root.Q<Button>("tool-remove");
            removeButton.clicked += toolbar.SelectRemove;

            foreach (PlaceableDefinition definition in toolbar.Catalogue)
            {
                string id = "tool-" + definition.Kind.ToString().ToLower();
                Button button = root.Q<Button>(id);
                if (button == null) continue;

                PlaceableDefinition captured = definition;
                button.clicked += () => toolbar.SelectPlace(captured);
                Label cost = root.Q<Label>(id + "-cost");
                if (cost != null) cost.text = "$" + definition.Cost;

                buttons[definition.Kind] = button;
                definitions[definition.Kind] = definition;
            }

            toolbar.ToolChanged += RenderSelection;
            toolbar.Feedback += RenderFeedback;
            game.Wallet.Changed += RenderAffordability;
            placement.CityPlaced += OnPlaced;

            RenderSelection();
            RenderAffordability(game.Wallet.Balance);
        }

        private void OnDestroy()
        {
            if (toolbar != null)
            {
                toolbar.ToolChanged -= RenderSelection;
                toolbar.Feedback -= RenderFeedback;
            }
            if (game != null && game.Wallet != null) game.Wallet.Changed -= RenderAffordability;
            if (placement != null) placement.CityPlaced -= OnPlaced;
        }

        private void OnPlaced(Transform city)
        {
            layer.RemoveFromClassList("hidden");
        }

        private void RenderSelection()
        {
            foreach (var pair in buttons)
            {
                bool selected = toolbar.Mode == ToolMode.Place && toolbar.Selected == definitions[pair.Key];
                pair.Value.EnableInClassList("tool--selected", selected);
            }
            removeButton.EnableInClassList("tool--selected", toolbar.Mode == ToolMode.Remove);

            if (toolbar.Mode == ToolMode.None)
            {
                RenderFeedback(new BuildFeedback("Pick a source, then tap a tile", true));
            }
        }

        private void RenderAffordability(float balance)
        {
            foreach (var pair in buttons)
            {
                pair.Value.EnableInClassList("tool--disabled", balance < definitions[pair.Key].Cost);
            }
        }

        private void RenderFeedback(BuildFeedback feedback)
        {
            hint.text = feedback.Message;
            hint.EnableInClassList("build-hint--bad", !feedback.Positive);
        }
    }
}
