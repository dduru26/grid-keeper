using GridKeeper.Core;
using GridKeeper.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace GridKeeper.Presentation
{
    public class DayNightCycle : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private Light sun;
        [SerializeField] private Material windowLitTemplate;

        [Header("Sun")]
        [SerializeField] private float dayIntensity = 1.15f;
        [SerializeField] private float nightIntensity = 0.12f;
        [SerializeField] private Gradient sunColour = DefaultSunColour();

        [Header("Ambient")]
        [SerializeField] private Color dayAmbient = new Color(0.72f, 0.76f, 0.8f);
        [SerializeField] private Color nightAmbient = new Color(0.17f, 0.19f, 0.3f);

        [Header("Windows")]
        [SerializeField] private float dayGlow = 0.35f;
        [SerializeField] private float nightGlow = 2.6f;

        public float Night01 { get; private set; }

        private Material windowLit;
        private Color windowBase;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            Require.Reference(this, game, nameof(game));
            RenderSettings.ambientMode = AmbientMode.Flat;

            if (windowLitTemplate != null)
            {
                windowLit = new Material(windowLitTemplate) { name = "WindowLit (Runtime)" };
                windowBase = windowLitTemplate.GetColor("_BaseColor");
                foreach (Building building in FindObjectsByType<Building>(FindObjectsInactive.Include))
                {
                    building.UseLitMaterial(windowLit);
                }
            }
        }

        private void OnDestroy()
        {
            if (windowLit != null) Destroy(windowLit);
        }

        private void Update()
        {
            if (game == null || game.Clock == null) return;
            float hour = game.Clock.SmoothHourOfDay;
            Apply(hour);
        }

        public static float SunHeight(float hour)
        {
            return Mathf.Sin((hour - 6f) / 12f * Mathf.PI);
        }

        private void Apply(float hour)
        {
            float height = SunHeight(hour);
            float day = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.15f, 0.25f, height));
            Night01 = 1f - day;

            if (sun != null)
            {
                float yaw = Mathf.Lerp(-110f, 110f, Mathf.Repeat(hour - 6f, 24f) / 12f);
                float pitch = Mathf.Lerp(8f, 62f, Mathf.Clamp01(height));
                sun.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, day);
                sun.color = sunColour.Evaluate(day);
                sun.shadowStrength = Mathf.Lerp(0.25f, 0.6f, day);
            }

            RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, day);

            if (windowLit != null)
            {
                windowLit.SetColor(EmissionColor, windowBase * Mathf.Lerp(dayGlow, nightGlow, Night01));
            }
        }

        private static Gradient DefaultSunColour()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.45f, 0.55f, 0.85f), 0f),
                    new GradientColorKey(new Color(1f, 0.62f, 0.38f), 0.35f),
                    new GradientColorKey(new Color(1f, 0.96f, 0.9f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}
