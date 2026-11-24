using System;
using GridKeeper.Data;
using GridKeeper.Energy;
using GridKeeper.World;
using UnityEngine;

namespace GridKeeper.Simulation
{
    public sealed class RunSimulation
    {
        public event Action<TickResult> Ticked;
        public event Action<int, DayOutcome> DayClosed;

        public GameClock Clock { get; private set; }
        public ShiftStats Stats { get; } = new ShiftStats();
        public int StrikesLeft => RunRules.StrikesLeft(balance, Stats.Strikes);

        private readonly BalanceConfig balance;
        private readonly City city;
        private readonly WeatherSystem weather;
        private readonly EnergyGrid grid = new EnergyGrid();
        private int trackedDay;
        private float dayDemandMWh;
        private float dayDeliveredMWh;
        private float pointRemainder;

        public RunSimulation(BalanceConfig balance, City city, WeatherSystem weather)
        {
            this.balance = balance;
            this.city = city;
            this.weather = weather;
            Clock = CreateClock();
        }

        public void Reset()
        {
            Clock = CreateClock();
            Stats.Reset();
            weather.Generate();
            trackedDay = 0;
            dayDemandMWh = dayDeliveredMWh = pointRemainder = 0f;
        }

        public TickResult Preview()
        {
            WeatherSample sample = weather.Sample(0f);
            float hourOfDay = balance.startHour;
            return new TickResult
            {
                HourOfDay = hourOfDay,
                DemandMW = city.DemandAt(hourOfDay, RunRules.DemandMultiplier(balance, 0, sample.heatwave))
            };
        }

        public void Advance(float deltaTime, Func<bool> shouldStop)
        {
            int ticks = Clock.Advance(deltaTime);
            for (int i = 0; i < ticks && !shouldStop(); i++)
            {
                float elapsedAtTickStart = Clock.ElapsedHours - (ticks - i) * Clock.TickHours;
                Step(Mathf.Max(0f, elapsedAtTickStart));
            }
        }

        private void Step(float elapsedHours)
        {
            float hours = Clock.TickHours;
            int dayIndex = Mathf.FloorToInt(elapsedHours / 24f);
            if (dayIndex > trackedDay) CloseDay();

            float hourOfDay = Mathf.Repeat(balance.startHour + elapsedHours, 24f);
            WeatherSample sample = weather.Sample(elapsedHours);
            float multiplier = RunRules.DemandMultiplier(balance, dayIndex, sample.heatwave);
            float demand = city.DemandAt(hourOfDay, multiplier);

            TickResult result = grid.Balance(demand, city.Generators, city.Storage, hourOfDay, sample, hours);
            result.ElapsedHours = elapsedHours;
            result.EarnedDollars = result.DeliveredMW * hours * balance.revenuePerMWh;

            Stats.Record(result, hours, result.EarnedDollars);
            dayDemandMWh += result.DemandMW * hours;
            dayDeliveredMWh += result.DeliveredMW * hours;
            AddPoints(result.DeliveredMW * hours * balance.pointsPerMWh);

            city.ApplySupply(result.DeliveredMW, hourOfDay, multiplier);
            Ticked?.Invoke(result);
        }

        private void AddPoints(float points)
        {
            pointRemainder += points;
            int whole = Mathf.FloorToInt(pointRemainder);
            if (whole <= 0) return;
            pointRemainder -= whole;
            Stats.AddPoints(whole);
        }

        private void CloseDay()
        {
            DayOutcome outcome = RunRules.ScoreDay(balance, dayDemandMWh, dayDeliveredMWh);
            Stats.AddPoints(outcome.Bonus);
            Stats.CompleteDay(outcome.Grade, outcome.Strike);
            trackedDay++;
            dayDemandMWh = dayDeliveredMWh = 0f;
            DayClosed?.Invoke(trackedDay, outcome);
        }

        private GameClock CreateClock()
        {
            return new GameClock(balance.startHour, balance.secondsPerDay, balance.tickHours);
        }
    }
}
