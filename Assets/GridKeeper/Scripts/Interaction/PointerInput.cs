using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GridKeeper.Interaction
{
    public static class PointerInput
    {
        public static int ActiveTouchCount
        {
            get
            {
                Touchscreen screen = Touchscreen.current;
                if (screen == null) return 0;
                int count = 0;
                foreach (var touch in screen.touches)
                {
                    if (touch.isInProgress) count++;
                }
                return count;
            }
        }

        public static bool TryGetPress(out Vector2 screenPosition)
        {
            screenPosition = default;
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return false;
            if (ActiveTouchCount > 1) return false;

            screenPosition = pointer.position.ReadValue();
            return !IsOverUI(screenPosition);
        }

        public static bool IsOverUI(Vector2 screenPosition)
        {
            UIDocument[] documents = Object.FindObjectsByType<UIDocument>();
            foreach (UIDocument document in documents)
            {
                VisualElement root = document.rootVisualElement;
                if (root?.panel == null) continue;

                Vector2 flipped = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
                Vector2 panelPoint = RuntimePanelUtils.ScreenToPanel(root.panel, flipped);
                VisualElement picked = root.panel.Pick(panelPoint);
                if (picked != null && picked != root) return true;
            }

            return false;
        }
    }
}
