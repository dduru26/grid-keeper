using UnityEngine;
using UnityEngine.UIElements;

namespace GridKeeper.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class FrameRateView : MonoBehaviour
    {
        [SerializeField, Tooltip("Show the meter in release builds too.")]
        private bool showInRelease;
        [SerializeField] private float refreshSeconds = 0.5f;

        private Label label;
        private float elapsed;
        private int frames;
        private float worstMs;

        private void Start()
        {
            label = GetComponent<UIDocument>().rootVisualElement.Q<Label>("fps");
            bool visible = Debug.isDebugBuild || showInRelease;
            label.EnableInClassList("hidden", !visible);
            enabled = visible;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            frames++;
            worstMs = Mathf.Max(worstMs, dt * 1000f);
            if (elapsed < refreshSeconds) return;

            float fps = frames / elapsed;
            label.text = Mathf.RoundToInt(fps) + " FPS · " + (elapsed / frames * 1000f).ToString("0.0") + " ms · worst " + worstMs.ToString("0") + " ms";
            label.EnableInClassList("fps--low", fps < Application.targetFrameRate * 0.9f);
            elapsed = 0f;
            frames = 0;
            worstMs = 0f;
        }
    }
}
