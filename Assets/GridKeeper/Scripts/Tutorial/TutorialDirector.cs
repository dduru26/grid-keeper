using System;
using GridKeeper.Construction;
using GridKeeper.Core;
using GridKeeper.Data;
using GridKeeper.Simulation;
using GridKeeper.World;
using UnityEngine;

namespace GridKeeper.Tutorial
{
    public enum TutorialStep
    {
        Off,
        BuildSolar,
        StartShift,
        WatchMorning,
        WatchNoon,
        BuildBattery,
        WatchStorage,
        ReadForecast,
        FreePlay
    }

    public class TutorialDirector : MonoBehaviour
    {
        private const string DoneKey = "gk.tutorial.done";

        [SerializeField] private GameManager game;
        [SerializeField] private BuildToolbar toolbar;
        [SerializeField] private float freePlayHintSeconds = 4f;
        [Header("When each hint appears, in in-game hours since the run started")]
        [SerializeField] private float noonAtHours = 5f;
        [SerializeField] private float batteryCheckAtHours = 10.5f;
        [SerializeField] private float forecastAtHours = 22f;
        [SerializeField] private float freePlayAtHours = 30f;

        public event Action<string, bool> HintChanged;

        public TutorialStep Step { get; private set; } = TutorialStep.Off;
        public bool IsActive => Step != TutorialStep.Off;

        private bool holding;
        private float freePlayTimer;

        public static bool Completed
        {
            get => PlayerPrefs.GetInt(DoneKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(DoneKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private void Awake()
        {
            if (game == null) game = FindAnyObjectByType<GameManager>();
            if (toolbar == null) toolbar = FindAnyObjectByType<BuildToolbar>();
        }

        private void OnEnable()
        {
            game.PhaseChanged += OnPhase;
            game.Ticked += OnTick;
            toolbar.Built += OnBuilt;
        }

        private void OnDisable()
        {
            game.PhaseChanged -= OnPhase;
            game.Ticked -= OnTick;
            toolbar.Built -= OnBuilt;
        }

        public void Skip()
        {
            Completed = true;
            Go(TutorialStep.Off);
        }

        public void Replay()
        {
            Completed = false;
            game.Restart();
        }

        private void OnPhase(GamePhase phase)
        {
            if (phase == GamePhase.Briefing)
            {
                Go(Completed ? TutorialStep.Off : TutorialStep.BuildSolar);
            }
            else if (phase == GamePhase.Playing && Step == TutorialStep.StartShift)
            {
                Go(TutorialStep.WatchMorning);
            }
            else if (phase == GamePhase.Results && IsActive)
            {
                Completed = true;
                Go(TutorialStep.Off);
            }
        }

        private void OnBuilt(Tile tile)
        {
            if (tile.Occupant == null) return;
            SourceKind kind = tile.Occupant.Definition.Kind;

            if (Step == TutorialStep.BuildSolar && kind == SourceKind.Solar)
            {
                Go(TutorialStep.StartShift);
            }
            else if (Step == TutorialStep.BuildBattery && kind == SourceKind.Battery)
            {
                Go(TutorialStep.WatchStorage);
            }
        }

        private void OnTick(TickResult tick)
        {
            if (game.Phase != GamePhase.Playing) return;
            float elapsed = tick.ElapsedHours;

            switch (Step)
            {
                case TutorialStep.WatchMorning when elapsed >= noonAtHours:
                    Go(TutorialStep.WatchNoon);
                    break;
                case TutorialStep.WatchNoon when elapsed >= batteryCheckAtHours:
                    if (game.City.Count(SourceKind.Battery) > 0) Go(TutorialStep.WatchStorage);
                    else Go(TutorialStep.BuildBattery);
                    break;
                case TutorialStep.WatchStorage when elapsed >= forecastAtHours:
                    Go(TutorialStep.ReadForecast);
                    break;
                case TutorialStep.ReadForecast when elapsed >= freePlayAtHours:
                    Go(TutorialStep.FreePlay);
                    break;
            }
        }

        private void Update()
        {
            if (Step != TutorialStep.FreePlay) return;
            freePlayTimer -= Time.deltaTime;
            if (freePlayTimer <= 0f)
            {
                Completed = true;
                Go(TutorialStep.Off);
            }
        }

        private void Go(TutorialStep next)
        {
            Step = next;
            SetHold(next == TutorialStep.BuildBattery);
            if (next == TutorialStep.FreePlay) freePlayTimer = freePlayHintSeconds;
            HintChanged?.Invoke(TextFor(next), next != TutorialStep.Off);
        }

        private void SetHold(bool hold)
        {
            if (hold == holding) return;
            holding = hold;
            if (hold) game.Hold();
            else game.Release();
        }

        private string TextFor(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.BuildSolar:
                    return "Start with the sun. Tap Solar, then tap a grass tile twice to build a panel.";
                case TutorialStep.StartShift:
                    return "Nice. Add more if you like, then press Start the shift.";
                case TutorialStep.WatchMorning:
                    return "Dawn. Watch the amber in the chart grow as your panels wake up.";
                case TutorialStep.WatchNoon:
                    return "Midday is solar at its strongest. Add panels until the amber in the chart reaches the white demand line.";
                case TutorialStep.BuildBattery:
                    return "Dusk is coming and the sun sets at 18:00. Build a battery to carry the town through the night.";
                case TutorialStep.WatchStorage:
                    return "Batteries charge from surplus and drain when supply falls short. Keep an eye on STORED.";
                case TutorialStep.ReadForecast:
                    return ForecastHint();
                case TutorialStep.FreePlay:
                    return "Hints are off now. Brightwater is yours, Keeper.";
                default:
                    return string.Empty;
            }
        }

        private string ForecastHint()
        {
            var forecast = game.Weather.Forecast;
            for (int i = 1; i < forecast.Count; i++)
            {
                if (forecast[i].Heatwave)
                {
                    return "Check the forecast chip. A heatwave is coming on day " + (i + 1) + ": demand rises 30% and the wind drops. Store energy early.";
                }
            }
            return "Check the forecast chip each morning. Cloud cuts solar, strong wind favours turbines on the hills.";
        }
    }
}
