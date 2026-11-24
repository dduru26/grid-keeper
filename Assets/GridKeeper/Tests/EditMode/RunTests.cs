using System.Reflection;
using GridKeeper.Core;
using GridKeeper.Data;
using GridKeeper.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace GridKeeper.Tests
{
    public class RunTests
    {
        private BalanceConfig balance;

        [SetUp]
        public void SetUp()
        {
            balance = ScriptableObject.CreateInstance<BalanceConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(balance);
        }

        [Test]
        public void ClockRunsOneDayInSecondsPerDay()
        {
            var clock = new GameClock(6f, 40f, 0.25f);
            clock.Advance(40f);
            Assert.AreEqual(24f, clock.ElapsedHours, 0.001f);
            Assert.AreEqual(2, clock.Day);
        }

        [Test]
        public void ClockNeverFinishes()
        {
            var clock = new GameClock(6f, 40f, 0.25f);
            clock.Advance(40f * 9f);
            Assert.AreEqual(216f, clock.ElapsedHours, 0.001f);
            Assert.AreEqual(10, clock.Day);
        }

        [Test]
        public void ClockIgnoresLessThanOneTick()
        {
            var clock = new GameClock(6f, 40f, 0.25f);
            Assert.AreEqual(0, clock.Advance(0.1f));
        }

        [Test]
        public void ClockFormatsTimes()
        {
            Assert.AreEqual("06:00", GameClock.Format(6f));
            Assert.AreEqual("18:30", GameClock.Format(18.5f));
            Assert.AreEqual("23:45", GameClock.Format(23.75f));
        }

        [Test]
        public void GradeBoundaries()
        {
            Assert.AreEqual(Grade.A, GradeTable.Evaluate(0.9f, 0.9f, 0.75f, 0.55f));
            Assert.AreEqual(Grade.B, GradeTable.Evaluate(0.8999f, 0.9f, 0.75f, 0.55f));
            Assert.AreEqual(Grade.C, GradeTable.Evaluate(0.55f, 0.9f, 0.75f, 0.55f));
            Assert.AreEqual(Grade.D, GradeTable.Evaluate(0.5499f, 0.9f, 0.75f, 0.55f));
        }

        [Test]
        public void DayBonusesMatchTheGrade()
        {
            Assert.AreEqual(300, RunRules.ScoreDay(balance, 100f, 95f).Bonus);
            Assert.AreEqual(150, RunRules.ScoreDay(balance, 100f, 80f).Bonus);
            Assert.AreEqual(50, RunRules.ScoreDay(balance, 100f, 60f).Bonus);
            Assert.AreEqual(0, RunRules.ScoreDay(balance, 100f, 40f).Bonus);
        }

        [Test]
        public void OnlyADDayCostsALife()
        {
            Assert.IsTrue(RunRules.ScoreDay(balance, 100f, 54f).Strike);
            Assert.IsFalse(RunRules.ScoreDay(balance, 100f, 56f).Strike);
        }

        [Test]
        public void ThreeStrikesLeaveNoLives()
        {
            Assert.AreEqual(3, RunRules.StrikesLeft(balance, 0));
            Assert.AreEqual(0, RunRules.StrikesLeft(balance, 3));
            Assert.AreEqual(0, RunRules.StrikesLeft(balance, 5));
        }

        [Test]
        public void DemandGrowsAndCapsAtDouble()
        {
            Assert.AreEqual(1f, RunRules.DemandMultiplier(balance, 0, false), 0.001f);
            Assert.AreEqual(1.04f, RunRules.DemandMultiplier(balance, 1, false), 0.001f);
            Assert.AreEqual(2f, RunRules.DemandMultiplier(balance, 100, false), 0.001f);
            Assert.AreEqual(1.3f, RunRules.DemandMultiplier(balance, 0, true), 0.001f);
        }

        [Test]
        public void ShareMetIsOneWithNoDemand()
        {
            Assert.AreEqual(1f, new ShiftStats().ShareMet);
        }

        [Test]
        public void StatsKeepAtMostSevenDaysOfSamples()
        {
            var stats = new ShiftStats();
            for (int i = 0; i < ShiftStats.MaxSamples + 50; i++) stats.Record(new TickResult { DemandMW = 1f, DeliveredMW = 1f }, 0.25f, 0f);
            Assert.AreEqual(ShiftStats.MaxSamples, stats.Samples.Count);
            Assert.AreEqual(1f, stats.ShareMet, 0.001f);
        }

        [Test]
        public void ScoreOnlyGoesUp()
        {
            var stats = new ShiftStats();
            stats.AddPoints(100);
            stats.AddPoints(50);
            Assert.AreEqual(150, stats.Score);
        }

        [Test]
        public void WalletRefusesWhatItCannotAfford()
        {
            var wallet = new Wallet(100f);
            Assert.IsFalse(wallet.TrySpend(150));
            Assert.AreEqual(100f, wallet.Balance);
            Assert.IsTrue(wallet.TrySpend(100));
            Assert.AreEqual(0f, wallet.Balance);
        }

        [Test]
        public void ForecastNeverRunsOutAndAvoidsBackToBackHeatwaves()
        {
            var go = new GameObject("Weather");
            var weather = go.AddComponent<WeatherSystem>();
            WeatherProfile clear = Profile(WeatherKind.Clear, false);
            WeatherProfile heat = Profile(WeatherKind.Heatwave, true);
            Set(weather, "clear", clear);
            Set(weather, "cloudy", Profile(WeatherKind.Cloudy, false));
            Set(weather, "windy", Profile(WeatherKind.Windy, false));
            Set(weather, "heatwave", heat);
            Set(weather, "fixedSeed", 7);

            weather.Generate();
            Assert.AreSame(clear, weather.ForDay(0));
            for (int day = 1; day < 200; day++)
            {
                Assert.IsNotNull(weather.ForDay(day));
                Assert.IsFalse(weather.ForDay(day) == heat && weather.ForDay(day - 1) == heat);
            }
            Object.DestroyImmediate(go);
        }

        private static WeatherProfile Profile(WeatherKind kind, bool heatwave)
        {
            var profile = ScriptableObject.CreateInstance<WeatherProfile>();
            profile.Configure(kind, kind.ToString(), string.Empty, Vector2.zero, Vector2.one, heatwave, Color.white);
            return profile;
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        }
    }
}
