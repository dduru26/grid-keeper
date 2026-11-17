using GridKeeper.AR;
using GridKeeper.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridKeeper.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class BudgetView : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private ARPlacementController placement;

        private VisualElement hud;
        private Label value;

        private void Start()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            hud = root.Q<VisualElement>("hud");
            value = root.Q<Label>("budget-value");

            if (game == null) game = FindAnyObjectByType<GameManager>();
            if (placement == null) placement = FindAnyObjectByType<ARPlacementController>();

            game.Wallet.Changed += Render;
            placement.CityPlaced += OnPlaced;
            Render(game.Wallet.Balance);
        }

        private void OnDestroy()
        {
            if (game != null && game.Wallet != null) game.Wallet.Changed -= Render;
            if (placement != null) placement.CityPlaced -= OnPlaced;
        }

        private void OnPlaced(Transform city)
        {
            hud.RemoveFromClassList("hidden");
        }

        private void Render(float balance)
        {
            value.text = "$" + Mathf.FloorToInt(balance).ToString("N0");
        }
    }
}
