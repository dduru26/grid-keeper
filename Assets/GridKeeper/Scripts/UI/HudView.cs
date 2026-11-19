using GridKeeper.Core;
using GridKeeper.Data;
using GridKeeper.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridKeeper.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class HudView : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField, Tooltip("Hours of history in the live chart.")]
        private float liveWindowHours = 24f;
        [SerializeField] private float bannerSeconds = 2.6f;
        [SerializeField] private float pulseSpeed = 3f;

        private VisualElement hud;
        private Label day;
        private Label time;
        private Label todayText;
        private Label nextText;
        private VisualElement todaySwatch;
        private VisualElement nextSwatch;
        private VisualElement nextChip;
        private Label supply;
        private Label demand;
        private Label stored;
        private Label banner;
        private VisualElement edge;
        private ShiftChart chart;
        private float bannerTimer;

        private void Start()
        {
            if (game == null) game = FindAnyObjectByType<GameManager>();
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            hud = root.Q<VisualElement>("hud");
            day = root.Q<Label>("clock-day");
            time = root.Q<Label>("clock-time");
            todayText = root.Q<Label>("forecast-today-text");
            nextText = root.Q<Label>("forecast-next-text");
            todaySwatch = root.Q<VisualElement>("forecast-today-swatch");
            nextSwatch = root.Q<VisualElement>("forecast-next-swatch");
            nextChip = root.Q<VisualElement>("forecast-next");
            supply = root.Q<Label>("stat-supply");
            demand = root.Q<Label>("stat-demand");
            stored = root.Q<Label>("stat-battery");
            banner = root.Q<Label>("day-banner");
            edge = root.Q<VisualElement>("blackout-edge");

            chart = new ShiftChart();
            chart.style.flexGrow = 1;
            root.Q<VisualElement>("live-chart").Add(chart);

            root.Q<Button>("pause-button").clicked += game.TogglePause;

            game.PhaseChanged += OnPhase;
            game.Ticked += OnTick;
            game.Weather.DayStarted += OnDayStarted;
            OnPhase(game.Phase);
        }

        private void OnDestroy()
        {
            if (game == null) return;
            game.PhaseChanged -= OnPhase;
            game.Ticked -= OnTick;
            if (game.Weather != null) game.Weather.DayStarted -= OnDayStarted;
        }

        private void OnPhase(GamePhase phase)
        {
            hud.EnableInClassList("hidden", phase == GamePhase.Placing);
            if (phase == GamePhase.Briefing)
            {
                RenderClock();
                RenderForecast(0);
                chart.SetData(game.Stats.Samples, liveWindowHours, liveWindowHours, true);
            }
        }

        private void OnTick(TickResult tick)
        {
            RenderClock();
            supply.text = "SUPPLY " + tick.DeliveredMW.ToString("0.0") + " MW";
            demand.text = "DEMAND " + tick.DemandMW.ToString("0.0") + " MW";
            stored.text = tick.CapacityMWh > 0f ? "STORED " + Mathf.RoundToInt(tick.Charge01 * 100f) + "%" : "NO STORAGE";
            chart.SetData(game.Stats.Samples, liveWindowHours, liveWindowHours, true);
        }

        private void OnDayStarted(int dayNumber, WeatherProfile profile)
        {
            RenderForecast(dayNumber - 1);
            if (game.Phase != GamePhase.Playing) return;
            banner.text = "Day " + dayNumber + " · " + profile.Label;
            banner.RemoveFromClassList("hidden");
            banner.RemoveFromClassList("day-banner--out");
            bannerTimer = bannerSeconds;
        }

        private void RenderClock()
        {
            day.text = "DAY " + game.Clock.Day + " OF " + game.Clock.Days;
            time.text = GameClock.Format(game.Clock.HourOfDay);
        }

        private void RenderForecast(int dayIndex)
        {
            WeatherProfile today = game.Weather.ForDay(dayIndex);
            todayText.text = "Today: " + today.Label;
            todaySwatch.style.backgroundColor = today.Tint;

            bool hasNext = dayIndex + 1 < game.Weather.Forecast.Count;
            nextChip.EnableInClassList("hidden", !hasNext);
            if (!hasNext) return;
            WeatherProfile next = game.Weather.ForDay(dayIndex + 1);
            nextText.text = "Tomorrow: " + next.Label;
            nextSwatch.style.backgroundColor = next.Tint;
        }

        private void Update()
        {
            if (game == null) return;

            float pulse = game.InBlackout ? 0.35f + 0.45f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI)) : 0f;
            edge.style.opacity = Mathf.MoveTowards(edge.resolvedStyle.opacity, pulse, Time.unscaledDeltaTime * 4f);

            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.unscaledDeltaTime;
                if (bannerTimer <= 0f) banner.AddToClassList("day-banner--out");
            }
        }
    }
}
