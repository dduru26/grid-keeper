namespace GridKeeper.Core
{
    public enum GamePhase
    {
        Placing,
        Briefing,
        Playing,
        Paused,
        Results
    }

    public abstract class GameState
    {
        protected readonly GameManager Game;

        protected GameState(GameManager game)
        {
            Game = game;
        }

        public abstract GamePhase Phase { get; }
        public virtual bool AllowsBuilding => false;

        public virtual void Enter()
        {
        }

        public virtual void Tick(float deltaTime)
        {
        }

        public virtual void Exit()
        {
        }
    }

    public sealed class PlacingState : GameState
    {
        public PlacingState(GameManager game) : base(game) { }
        public override GamePhase Phase => GamePhase.Placing;
    }

    public sealed class BriefingState : GameState
    {
        public BriefingState(GameManager game) : base(game) { }
        public override GamePhase Phase => GamePhase.Briefing;
        public override bool AllowsBuilding => true;

        public override void Enter()
        {
            Game.PrepareShift();
        }
    }

    public sealed class PlayingState : GameState
    {
        public PlayingState(GameManager game) : base(game) { }
        public override GamePhase Phase => GamePhase.Playing;
        public override bool AllowsBuilding => true;

        public override void Tick(float deltaTime)
        {
            if (Game.IsHeld) return;
            Game.Simulate(deltaTime);
            if (Game.RunOver) Game.ChangeState(Game.Results);
        }
    }

    public sealed class PausedState : GameState
    {
        public PausedState(GameManager game) : base(game) { }
        public override GamePhase Phase => GamePhase.Paused;
    }

    public sealed class ResultsState : GameState
    {
        public ResultsState(GameManager game) : base(game) { }
        public override GamePhase Phase => GamePhase.Results;

        public override void Enter()
        {
            Game.FinishShift();
        }
    }
}
