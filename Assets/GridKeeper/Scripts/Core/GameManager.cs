using System;
using GridKeeper.AR;
using GridKeeper.Construction;
using GridKeeper.Data;
using GridKeeper.Energy;
using GridKeeper.Simulation;
using GridKeeper.World;
using UnityEngine;

namespace GridKeeper.Core
{
    public readonly struct ShiftResult
    {
        public readonly Grade Grade;
        public readonly float ShareMet;
        public readonly string CouncilNote;
        public readonly ShiftStats Stats;

        public ShiftResult(Grade grade, float shareMet, string note, ShiftStats stats)
        {
            Grade = grade;
            ShareMet = shareMet;
            CouncilNote = note;
            Stats = stats;
        }
    }

    public class GameManager : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;
        [SerializeField] private City city;
        [SerializeField] private ARPlacementController placement;
        [SerializeField] private WeatherSystem weather;
        [SerializeField] private SourceFactory factory;
        [SerializeField] private BuildToolbar toolbar;

        public event Action<GamePhase> PhaseChanged;
        public event Action<TickResult> Ticked;
        public event Action<bool> BlackoutChanged;
        public event Action<ShiftResult> ShiftEnded;

        public BalanceConfig Balance => balance;
        public City City => city;
        public WeatherSystem Weather => weather;
        public Wallet Wallet { get; private set; }
        public GameClock Clock { get; private set; }
        public ShiftStats Stats { get; } = new ShiftStats();
        public TickResult LastTick { get; private set; }
        public bool InBlackout { get; private set; }
        public bool IsHeld => holds > 0;
        public ShiftResult LastResult { get; private set; }

        public GameState State { get; private set; }
        public GamePhase Phase => State.Phase;

        public PlacingState Placing { get; private set; }
        public BriefingState Briefing { get; private set; }
        public PlayingState Playing { get; private set; }
        public PausedState Paused { get; private set; }
        public ResultsState Results { get; private set; }

        private readonly EnergyGrid grid = new EnergyGrid();
        private int holds;

        public void Hold()
        {
            holds++;
        }

        public void Release()
        {
            holds = Mathf.Max(0, holds - 1);
        }

        private void Awake()
        {
            if (city == null) city = FindAnyObjectByType<City>(FindObjectsInactive.Include);
            if (placement == null) placement = FindAnyObjectByType<ARPlacementController>();
            if (weather == null) weather = FindAnyObjectByType<WeatherSystem>();
            if (factory == null) factory = FindAnyObjectByType<SourceFactory>();
            if (toolbar == null) toolbar = FindAnyObjectByType<BuildToolbar>();

            Wallet = new Wallet(balance.startingBudget);
            Clock = CreateClock();

            Placing = new PlacingState(this);
            Briefing = new BriefingState(this);
            Playing = new PlayingState(this);
            Paused = new PausedState(this);
            Results = new ResultsState(this);
            State = Placing;
        }

        private void OnEnable()
        {
            if (placement != null) placement.CityPlaced += OnCityPlaced;
        }

        private void OnDisable()
        {
            if (placement != null) placement.CityPlaced -= OnCityPlaced;
        }

        private void Start()
        {
            ApplyBuildPermission();
            PhaseChanged?.Invoke(State.Phase);
        }

        private void Update()
        {
            State.Tick(Time.deltaTime);
        }

        public void ChangeState(GameState next)
        {
            if (next == null || next == State) return;
            State.Exit();
            State = next;
            State.Enter();
            ApplyBuildPermission();
            PhaseChanged?.Invoke(State.Phase);
        }

        public void StartShift()
        {
            if (State == Briefing) ChangeState(Playing);
        }

        public void TogglePause()
        {
            if (State == Playing) ChangeState(Paused);
            else if (State == Paused) ChangeState(Playing);
        }

        public void Restart()
        {
            if (State == Placing) return;
            foreach (Placeable placeable in new System.Collections.Generic.List<Placeable>(city.Placeables))
            {
                factory.Remove(placeable.Tile);
            }
            Wallet.Reset(balance.startingBudget);
            State.Exit();
            State = Placing;
            ChangeState(Briefing);
        }

        public void PrepareShift()
        {
            holds = 0;
            Clock = CreateClock();
            Stats.Reset();
            weather.Generate(balance.shiftDays);
            LastTick = default;
            SetBlackout(false);
            city.LightAll();
            Preview();
        }

        public void Simulate(float deltaTime)
        {
            int ticks = Clock.Advance(deltaTime);
            for (int i = 0; i < ticks; i++)
            {
                float elapsedAtTickStart = Clock.ElapsedHours - (ticks - i) * Clock.TickHours;
                RunTick(Mathf.Max(0f, elapsedAtTickStart));
            }
        }

        public void FinishShift()
        {
            Grade grade = GradeTable.Evaluate(Stats.ShareMet, balance.gradeA, balance.gradeB, balance.gradeC);
            LastResult = new ShiftResult(grade, Stats.ShareMet, GradeTable.CouncilNote(grade), Stats);
            SetBlackout(false);
            ShiftEnded?.Invoke(LastResult);
        }

        private void RunTick(float elapsedHours)
        {
            float hours = Clock.TickHours;
            float hourOfDay = Mathf.Repeat(balance.startHour + elapsedHours, 24f);
            WeatherSample sample = weather.Sample(elapsedHours);
            float multiplier = sample.heatwave ? balance.heatwaveDemandFactor : 1f;
            float demand = city.DemandAt(hourOfDay, multiplier);

            TickResult result = grid.Balance(demand, city.Placeables, hourOfDay, sample, hours);
            result.ElapsedHours = elapsedHours;

            float earned = result.DeliveredMW * hours * balance.revenuePerMWh;
            Wallet.Add(earned);
            Stats.Record(result, hours, earned);

            city.ApplySupply(result.DeliveredMW, hourOfDay, multiplier);
            SetBlackout(result.IsBlackout);

            LastTick = result;
            Ticked?.Invoke(result);
        }

        private void Preview()
        {
            WeatherSample sample = weather.Sample(0f);
            float hourOfDay = balance.startHour;
            float multiplier = sample.heatwave ? balance.heatwaveDemandFactor : 1f;
            var preview = new TickResult
            {
                HourOfDay = hourOfDay,
                DemandMW = city.DemandAt(hourOfDay, multiplier)
            };
            LastTick = preview;
            Ticked?.Invoke(preview);
        }

        private void SetBlackout(bool blackout)
        {
            if (InBlackout == blackout) return;
            InBlackout = blackout;
            BlackoutChanged?.Invoke(blackout);
        }

        private void OnCityPlaced(Transform root)
        {
            if (State == Placing) ChangeState(Briefing);
        }

        private void ApplyBuildPermission()
        {
            if (toolbar == null) return;
            toolbar.Interactable = State.AllowsBuilding;
            if (!State.AllowsBuilding) toolbar.Deselect();
        }

        private GameClock CreateClock()
        {
            return new GameClock(balance.startHour, balance.shiftDays, balance.shiftSeconds, balance.tickHours);
        }
    }
}
