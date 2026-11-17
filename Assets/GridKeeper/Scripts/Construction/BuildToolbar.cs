using System;
using GridKeeper.AR;
using GridKeeper.Core;
using GridKeeper.Data;
using GridKeeper.Interaction;
using GridKeeper.World;
using UnityEngine;

namespace GridKeeper.Construction
{
    public enum ToolMode
    {
        None,
        Place,
        Remove
    }

    public readonly struct BuildFeedback
    {
        public readonly string Message;
        public readonly bool Positive;

        public BuildFeedback(string message, bool positive)
        {
            Message = message;
            Positive = positive;
        }
    }

    public class BuildToolbar : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private ARPlacementController placement;
        [SerializeField] private SourceFactory factory;
        [SerializeField] private GhostPreview ghost;
        [SerializeField] private PinchToScale pinch;
        [SerializeField] private PlaceableDefinition[] catalogue = new PlaceableDefinition[0];
        [SerializeField] private LayerMask tileMask = ~0;
        [SerializeField] private float maxRayDistance = 10f;

        public event Action ToolChanged;
        public event Action<BuildFeedback> Feedback;
        public event Action<Tile> Built;
        public event Action<Tile> Removed;

        public PlaceableDefinition[] Catalogue => catalogue;
        public ToolMode Mode { get; private set; } = ToolMode.None;
        public PlaceableDefinition Selected { get; private set; }
        public bool Interactable { get; set; } = true;

        private Tile pending;
        private Camera cam;

        private void Awake()
        {
            if (game == null) game = FindAnyObjectByType<GameManager>();
            if (placement == null) placement = FindAnyObjectByType<ARPlacementController>();
            if (factory == null) factory = GetComponent<SourceFactory>();
            if (ghost == null) ghost = GetComponentInChildren<GhostPreview>(true);
            if (pinch == null) pinch = FindAnyObjectByType<PinchToScale>();
            cam = Camera.main;
        }

        public void SelectPlace(PlaceableDefinition definition)
        {
            bool toggleOff = Mode == ToolMode.Place && Selected == definition;
            ClearPending();
            Mode = toggleOff ? ToolMode.None : ToolMode.Place;
            Selected = toggleOff ? null : definition;
            Haptics.Selection();
            ToolChanged?.Invoke();
            if (!toggleOff) Feedback?.Invoke(new BuildFeedback("Tap a tile to preview " + definition.DisplayName.ToLower(), true));
        }

        public void SelectRemove()
        {
            bool toggleOff = Mode == ToolMode.Remove;
            ClearPending();
            Mode = toggleOff ? ToolMode.None : ToolMode.Remove;
            Selected = null;
            Haptics.Selection();
            ToolChanged?.Invoke();
            if (!toggleOff) Feedback?.Invoke(new BuildFeedback("Tap a source to remove it for a half refund", true));
        }

        public void Deselect()
        {
            ClearPending();
            Mode = ToolMode.None;
            Selected = null;
            ToolChanged?.Invoke();
        }

        private void Update()
        {
            if (!Interactable || Mode == ToolMode.None) return;
            if (placement != null && placement.State != PlacementState.Placed) return;
            if (pinch != null && pinch.IsPinching) return;
            if (!PointerInput.TryGetPress(out Vector2 screen)) return;

            if (cam == null) cam = Camera.main;
            Ray ray = cam.ScreenPointToRay(screen);
            Tile tile = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, tileMask)
                ? hit.collider.GetComponentInParent<Tile>()
                : null;

            if (tile == null)
            {
                ClearPending();
                return;
            }

            if (Mode == ToolMode.Place) HandlePlace(tile);
            else HandleRemove(tile);
        }

        private void HandlePlace(Tile tile)
        {
            bool canAfford = game.Wallet.CanAfford(Selected.Cost);
            bool valid = tile.IsBuildable && canAfford;

            if (tile != pending)
            {
                pending = tile;
                ghost.Show(Selected, tile, valid);
                Haptics.Selection();
                Feedback?.Invoke(Explain(tile, canAfford));
                return;
            }

            if (!valid)
            {
                Haptics.Error();
                Feedback?.Invoke(Explain(tile, canAfford));
                return;
            }

            game.Wallet.TrySpend(Selected.Cost);
            factory.Create(Selected, tile);
            ClearPending();
            Haptics.Tick();
            Feedback?.Invoke(new BuildFeedback(Selected.DisplayName + " built for $" + Selected.Cost, true));
            Built?.Invoke(tile);
        }

        private void HandleRemove(Tile tile)
        {
            if (tile.Occupant == null)
            {
                ClearPending();
                Feedback?.Invoke(new BuildFeedback("Nothing to remove there", false));
                return;
            }

            if (tile != pending)
            {
                pending = tile;
                ghost.ShowRemoval(tile);
                Haptics.Selection();
                Feedback?.Invoke(new BuildFeedback("Tap again to remove for $" + Refund(tile), true));
                return;
            }

            int refund = Refund(tile);
            factory.Remove(tile);
            game.Wallet.Add(refund);
            ClearPending();
            Haptics.Tick();
            Feedback?.Invoke(new BuildFeedback("Removed. $" + refund + " refunded", true));
            Removed?.Invoke(tile);
        }

        private int Refund(Tile tile)
        {
            float fraction = game.Balance != null ? game.Balance.refundFraction : 0.5f;
            return Mathf.FloorToInt(tile.Occupant.Definition.Cost * fraction);
        }

        private BuildFeedback Explain(Tile tile, bool canAfford)
        {
            if (tile.Occupant != null || tile.Kind == TileKind.Built) return new BuildFeedback("That tile is taken", false);
            if (tile.Kind == TileKind.Water) return new BuildFeedback("You can't build on the bay", false);
            if (!canAfford) return new BuildFeedback("Not enough money for " + Selected.DisplayName.ToLower(), false);
            string bonus = Selected.Kind == SourceKind.Wind && tile.IsHill ? ". Hills add 40% wind" : string.Empty;
            return new BuildFeedback("Tap again to build for $" + Selected.Cost + bonus, true);
        }

        private void ClearPending()
        {
            pending = null;
            if (ghost != null) ghost.Hide();
        }
    }
}
