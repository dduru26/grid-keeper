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
        public readonly int BestScore;
        public readonly bool NewBest;

        public ShiftResult(Grade grade, float shareMet, string note, ShiftStats stats, int bestScore, bool newBest)
        {
            Grade = grade;
            ShareMet = shareMet;
            CouncilNote = note;
            Stats = stats;
            BestScore = bestScore;
            NewBest = newBest;
        }
    }

    public readonly struct DayReport
    {
        public readonly int Day;
        public readonly Grade Grade;
        public readonly float ShareMet;
        public readonly int Bonus;
        public readonly bool Strike;
        public readonly int StrikesLeft;

        public DayReport(int day, Grade grade, float shareMet, int bonus, bool strike, int strikesLeft)
        {
            Day = day;
            Grade = grade;
            ShareMet = shareMet;
            Bonus = bonus;
            Strike = strike;
            StrikesLeft = strikesLeft;
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
        public event Action<DayReport> DayCompleted;

        private const string BestScoreKey = "gk.best.score";

        public static int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);
        public int StrikesLeft => run.StrikesLeft;
        public bool RunOver => endRequested || StrikesLeft <= 0;

        public BalanceConfig Balance => balance;
        public City City => city;
        public WeatherSystem Weather => weather;
        public Wallet Wallet { get; private set; }
        public GameClock Clock => run.Clock;
        public ShiftStats Stats => run.Stats;
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

        private RunSimulation run;
        private Func<bool> isRunOver;
        private int holds;
        private bool endRequested;

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
            run = new RunSimulation(balance, city, weather);
            run.Ticked += OnRunTicked;
            run.DayClosed += OnDayClosed;
            isRunOver = () => RunOver;

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

        public void EndRun()
        {
            if (State != Playing && State != Paused) return;
            endRequested = true;
            ChangeState(Results);
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
            run.Reset();
            endRequested = false;
            SetBlackout(false);
            city.LightAll();
            LastTick = run.Preview();
            Ticked?.Invoke(LastTick);
        }

        public void Simulate(float deltaTime)
        {
            run.Advance(deltaTime, isRunOver);
        }

        public void FinishShift()
        {
            Grade grade = GradeTable.Evaluate(Stats.ShareMet, balance.gradeA, balance.gradeB, balance.gradeC);
            int best = BestScore;
            bool newBest = Stats.Score > best;
            if (newBest)
            {
                best = Stats.Score;
                PlayerPrefs.SetInt(BestScoreKey, best);
                PlayerPrefs.Save();
            }
            LastResult = new ShiftResult(grade, Stats.ShareMet, GradeTable.CouncilNote(grade), Stats, best, newBest);
            SetBlackout(false);
            ShiftEnded?.Invoke(LastResult);
        }

        private void OnRunTicked(TickResult result)
        {
            Wallet.Add(result.EarnedDollars);
            SetBlackout(result.IsBlackout);
            LastTick = result;
            Ticked?.Invoke(result);
        }

        private void OnDayClosed(int day, DayOutcome outcome)
        {
            DayCompleted?.Invoke(new DayReport(day, outcome.Grade, outcome.ShareMet, outcome.Bonus, outcome.Strike, StrikesLeft));
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
    }
}
