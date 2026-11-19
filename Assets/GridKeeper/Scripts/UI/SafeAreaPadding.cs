using UnityEngine;
using UnityEngine.UIElements;

namespace GridKeeper.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SafeAreaPadding : MonoBehaviour
    {
        private VisualElement root;
        private Rect lastSafeArea;
        private Vector2Int lastScreen;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height) Apply();
        }

        private void Apply()
        {
            if (root?.panel == null) return;
            lastSafeArea = Screen.safeArea;
            lastScreen = new Vector2Int(Screen.width, Screen.height);

            Vector2 topLeft = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(lastSafeArea.xMin, Screen.height - lastSafeArea.yMax));
            Vector2 bottomRight = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(lastSafeArea.xMax, Screen.height - lastSafeArea.yMin));
            Vector2 full = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(Screen.width, Screen.height));

            root.style.position = Position.Absolute;
            root.style.top = topLeft.y;
            root.style.left = topLeft.x;
            root.style.right = full.x - bottomRight.x;
            root.style.bottom = full.y - bottomRight.y;
        }
    }
}
