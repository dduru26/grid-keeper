using GridKeeper.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridKeeper.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class BudgetView : MonoBehaviour
    {
        [SerializeField] private GameManager game;

        private Label value;

        private void Start()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            value = root.Q<Label>("budget-value");

            if (game == null) game = FindAnyObjectByType<GameManager>();

            game.Wallet.Changed += Render;
            Render(game.Wallet.Balance);
        }

        private void OnDestroy()
        {
            if (game != null && game.Wallet != null) game.Wallet.Changed -= Render;
        }

        private void Render(float balance)
        {
            value.text = "$" + Mathf.FloorToInt(balance).ToString("N0");
        }
    }
}
