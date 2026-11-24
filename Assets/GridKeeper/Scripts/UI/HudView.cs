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
        private VisualElement statusPill;
        private Label statusText;
        private Label statusGrade;
        private VisualElement meterFill;
        private string shownStatus;
        private int shownMeter = -1;
        private int shownScore = -1;
        private float bannerTimer;
        private string pendingDayBanner;
        private int shownSupply = -1;
        private int shownDemand = -1;
        private int shownStored = -2;
        private int shownMinute = -1;

        private void Start()
        {
            Require.Reference(this, game, nameof(game));
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
            statusPill = root.Q<VisualElement>("status-pill");
            statusText = root.Q<Label>("status-text");
            statusGrade = root.Q<Label>("status-grade");
            meterFill = root.Q<VisualElement>("meter-fill");

            chart = new ShiftChart();
            chart.style.flexGrow = 1;
            root.Q<VisualElement>("live-chart").Add(chart);

            root.Q<Button>("pause-button").clicked += game.TogglePause;

            game.PhaseChanged += OnPhase;
            game.Ticked += OnTick;
            game.Weather.DayStarted += OnDayStarted;
            game.DayCompleted += OnDayCompleted;
            OnPhase(game.Phase);
        }

        private void OnDestroy()
        {
            if (game == null) return;
            game.PhaseChanged -= OnPhase;
            game.Ticked -= OnTick;
            if (game.Weather != null) game.Weather.DayStarted -= OnDayStarted;
            game.DayCompleted -= OnDayCompleted;
        }

        private void OnPhase(GamePhase phase)
        {
            hud.EnableInClassList("hidden", phase == GamePhase.Placing);
            if (phase == GamePhase.Briefing)
            {
                shownMinute = shownSupply = shownDemand = -1;
                shownStored = -2;
                shownMeter = -1;
                SetStatus("ready", "PLAN YOUR GRID");
                statusGrade.text = "SCORE 0";
                shownScore = -1;
                meterFill.style.width = Length.Percent(0f);
                RenderClock();
                RenderForecast(0);
                chart.SetData(game.Stats.Samples, liveWindowHours, liveWindowHours, true);
            }
        }

        private void OnTick(TickResult tick)
        {
            RenderClock();

            int supplyTenths = Mathf.RoundToInt(tick.DeliveredMW * 10f);
            if (supplyTenths != shownSupply)
            {
                shownSupply = supplyTenths;
                supply.text = "SUPPLY " + (supplyTenths / 10f).ToString("0.0") + " MW";
            }

            int demandTenths = Mathf.RoundToInt(tick.DemandMW * 10f);
            if (demandTenths != shownDemand)
            {
                shownDemand = demandTenths;
                demand.text = "DEMAND " + (demandTenths / 10f).ToString("0.0") + " MW";
            }

            int storedPercent = tick.CapacityMWh > 0f ? Mathf.RoundToInt(tick.Charge01 * 100f) : -1;
            if (storedPercent != shownStored)
            {
                shownStored = storedPercent;
                stored.text = storedPercent < 0 ? "NO STORAGE" : "STORED " + storedPercent + "%";
            }

            RenderStatus(tick);
            chart.SetData(game.Stats.Samples, liveWindowHours, liveWindowHours, true);
        }

        private void RenderStatus(TickResult tick)
        {
            if (tick.IsBlackout)
            {
                SetStatus("short", "SHORT " + tick.UnmetMW.ToString("0.0") + " MW");
            }
            else if (tick.ChargedMW > 0.05f || tick.WastedMW > 0.05f)
            {
                SetStatus("surplus", "SURPLUS +" + (tick.ChargedMW + tick.WastedMW).ToString("0.0") + " MW");
            }
            else
            {
                SetStatus("ok", "TOWN POWERED");
            }

            int meter = tick.DemandMW <= 0f ? 100 : Mathf.RoundToInt(Mathf.Clamp01(tick.DeliveredMW / tick.DemandMW) * 100f);
            if (meter != shownMeter)
            {
                shownMeter = meter;
                meterFill.style.width = Length.Percent(meter);
                meterFill.style.backgroundColor = meter >= 98 ? new Color(0.36f, 0.75f, 0.45f) : meter >= 75 ? new Color(0.949f, 0.647f, 0.086f) : new Color(0.84f, 0.27f, 0.23f);
            }

            RenderScore();
        }

        private void RenderScore()
        {
            int score = game.Stats.Score;
            if (score == shownScore) return;
            shownScore = score;
            statusGrade.text = "SCORE " + score.ToString("N0");
        }

        private void OnDayCompleted(DayReport report)
        {
            RenderScore();
            shownMinute = -1;
            RenderClock();
            string text = report.Strike
                ? "Day " + report.Day + " went dark · " + report.StrikesLeft + (report.StrikesLeft == 1 ? " life left" : " lives left")
                : "Day " + report.Day + " grade " + report.Grade + " · +" + report.Bonus + " bonus";
            ShowBanner(text);
        }

        private void ShowBanner(string text)
        {
            if (game.Phase != GamePhase.Playing) return;
            banner.text = text;
            banner.RemoveFromClassList("hidden");
            banner.RemoveFromClassList("day-banner--out");
            bannerTimer = bannerSeconds;
        }

        private void SetStatus(string kind, string text)
        {
            string key = kind + text;
            if (key == shownStatus) return;
            shownStatus = key;
            statusPill.EnableInClassList("status--ok", kind == "ok");
            statusPill.EnableInClassList("status--surplus", kind == "surplus");
            statusPill.EnableInClassList("status--short", kind == "short");
            statusText.text = text;
        }

        private void OnDayStarted(int dayNumber, WeatherProfile profile)
        {
            RenderForecast(dayNumber - 1);
            if (dayNumber > 1 && game.Phase == GamePhase.Playing) pendingDayBanner = "Day " + dayNumber + " · " + profile.Label;
        }

        private void RenderClock()
        {
            int minute = Mathf.FloorToInt(game.Clock.ElapsedHours * 60f);
            if (minute == shownMinute) return;
            shownMinute = minute;
            day.text = "DAY " + game.Clock.Day + " · " + game.StrikesLeft + (game.StrikesLeft == 1 ? " LIFE" : " LIVES");
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
            else if (pendingDayBanner != null)
            {
                string next = pendingDayBanner;
                pendingDayBanner = null;
                ShowBanner(next);
            }
        }
    }
}
