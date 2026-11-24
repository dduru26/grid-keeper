using System.Collections.Generic;
using GridKeeper.Energy;
using GridKeeper.Simulation;
using NUnit.Framework;

namespace GridKeeper.Tests
{
    public class EnergyTests
    {
        private sealed class FakeGenerator : IGenerator
        {
            private readonly float output;

            public FakeGenerator(GeneratorKind kind, float outputMW)
            {
                Kind = kind;
                output = outputMW;
            }

            public GeneratorKind Kind { get; }

            public float Generate(float hour, WeatherSample weather)
            {
                return output;
            }
        }

        private const float Tick = 0.25f;
        private readonly EnergyGrid grid = new EnergyGrid();

        private TickResult Run(float demand, IReadOnlyList<IGenerator> generators, IReadOnlyList<IStorage> storage)
        {
            return grid.Balance(demand, generators, storage, 12f, WeatherSample.Clear, Tick);
        }

        [Test]
        public void SurplusChargesBatteryWithoutWaste()
        {
            var battery = new BatteryStore(30f, 8f);
            TickResult result = Run(10f, new[] { new FakeGenerator(GeneratorKind.Solar, 14f) }, new[] { battery });
            Assert.AreEqual(4f, result.ChargedMW, 0.001f);
            Assert.AreEqual(0f, result.WastedMW, 0.001f);
            Assert.AreEqual(1f, battery.StoredMWh, 0.001f);
        }

        [Test]
        public void SurplusBeyondRateIsWasted()
        {
            var battery = new BatteryStore(30f, 8f);
            TickResult result = Run(0f, new[] { new FakeGenerator(GeneratorKind.Solar, 20f) }, new[] { battery });
            Assert.AreEqual(8f, result.ChargedMW, 0.001f);
            Assert.AreEqual(12f, result.WastedMW, 0.001f);
        }

        [Test]
        public void ShortfallIsCoveredByDischarge()
        {
            var battery = new BatteryStore(30f, 8f, 30f);
            TickResult result = Run(10f, new[] { new FakeGenerator(GeneratorKind.Wind, 4f) }, new[] { battery });
            Assert.AreEqual(6f, result.DischargedMW, 0.001f);
            Assert.AreEqual(10f, result.DeliveredMW, 0.001f);
            Assert.IsFalse(result.IsBlackout);
        }

        [Test]
        public void EmptyBatteryLeavesUnmetDemandAndBlackout()
        {
            var battery = new BatteryStore(30f, 8f);
            TickResult result = Run(10f, new[] { new FakeGenerator(GeneratorKind.Solar, 2f) }, new[] { battery });
            Assert.AreEqual(8f, result.UnmetMW, 0.001f);
            Assert.IsTrue(result.IsBlackout);
        }

        [Test]
        public void SurplusIsSharedAcrossBatteries()
        {
            var a = new BatteryStore(30f, 8f);
            var b = new BatteryStore(30f, 8f);
            Run(0f, new[] { new FakeGenerator(GeneratorKind.Solar, 10f) }, new IStorage[] { a, b });
            Assert.AreEqual(a.StoredMWh, b.StoredMWh, 0.001f);
            Assert.AreEqual(2.5f, a.StoredMWh + b.StoredMWh, 0.001f);
        }

        [Test]
        public void GeneratorsAreSplitBySourceKind()
        {
            TickResult result = Run(0f, new IGenerator[] { new FakeGenerator(GeneratorKind.Solar, 3f), new FakeGenerator(GeneratorKind.Wind, 5f) }, new IStorage[0]);
            Assert.AreEqual(3f, result.SolarMW, 0.001f);
            Assert.AreEqual(5f, result.WindMW, 0.001f);
        }

        [Test]
        public void BatteryNeverMovesMoreThanItsRateInOneTick()
        {
            var battery = new BatteryStore(30f, 8f);
            battery.BeginTick();
            float first = battery.Charge(6f, Tick);
            float second = battery.Charge(6f, Tick);
            Assert.AreEqual(8f, first + second, 0.001f);
        }

        [Test]
        public void BatteryRateResetsEachTick()
        {
            var battery = new BatteryStore(30f, 8f);
            battery.BeginTick();
            battery.Charge(8f, Tick);
            battery.BeginTick();
            Assert.AreEqual(8f, battery.Charge(8f, Tick), 0.001f);
        }

        [Test]
        public void BatteryNeverOverfills()
        {
            var battery = new BatteryStore(1f, 8f, 0.5f);
            battery.Charge(8f, Tick);
            Assert.AreEqual(1f, battery.StoredMWh, 0.001f);
        }

        [Test]
        public void SunFactorFollowsTheDay()
        {
            Assert.AreEqual(0f, SolarPanel.SunFactor(6f), 0.001f);
            Assert.AreEqual(1f, SolarPanel.SunFactor(12f), 0.001f);
            Assert.AreEqual(0f, SolarPanel.SunFactor(18f), 0.001f);
            Assert.AreEqual(0f, SolarPanel.SunFactor(23f), 0.001f);
        }
    }
}
