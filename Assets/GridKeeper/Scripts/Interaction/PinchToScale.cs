using GridKeeper.AR;
using GridKeeper.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GridKeeper.Interaction
{
    public class PinchToScale : MonoBehaviour
    {
        [SerializeField] private ARPlacementController placement;
        [SerializeField] private BalanceConfig balance;
        [SerializeField, Tooltip("Scale change per scroll notch when testing in the Editor.")]
        private float scrollStep = 0.08f;

        public bool IsPinching { get; private set; }

        private float startDistance;
        private float startScale;

        private void Awake()
        {
            if (placement == null) placement = FindAnyObjectByType<ARPlacementController>();
        }

        private void Update()
        {
            if (placement == null || placement.State != PlacementState.Placed) return;
            Transform city = placement.CityRoot;
            if (city == null) return;

            if (TryGetTwoTouches(out Vector2 a, out Vector2 b))
            {
                float distance = Vector2.Distance(a, b);
                if (!IsPinching)
                {
                    IsPinching = true;
                    startDistance = Mathf.Max(1f, distance);
                    startScale = city.localScale.x;
                }

                Apply(city, startScale * distance / startDistance);
                return;
            }

            IsPinching = false;

            Mouse mouse = Mouse.current;
            if (mouse != null && Application.isEditor)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    Apply(city, city.localScale.x * (1f + Mathf.Sign(scroll) * scrollStep));
                }
            }
        }

        private void Apply(Transform city, float scale)
        {
            float min = balance != null ? balance.minCityScale : 0.5f;
            float max = balance != null ? balance.maxCityScale : 2.5f;
            city.localScale = Vector3.one * Mathf.Clamp(scale, min, max);
        }

        private static bool TryGetTwoTouches(out Vector2 a, out Vector2 b)
        {
            a = b = default;
            Touchscreen screen = Touchscreen.current;
            if (screen == null) return false;

            int found = 0;
            foreach (var touch in screen.touches)
            {
                if (!touch.isInProgress) continue;
                if (found == 0) a = touch.position.ReadValue();
                else b = touch.position.ReadValue();
                found++;
                if (found == 2) return true;
            }
            return false;
        }
    }
}
