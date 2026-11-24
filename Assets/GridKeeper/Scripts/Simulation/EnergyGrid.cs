using System.Collections.Generic;
using GridKeeper.Energy;

namespace GridKeeper.Simulation
{
    public sealed class EnergyGrid
    {
        public TickResult Balance(float demandMW, IReadOnlyList<IGenerator> generators, IReadOnlyList<IStorage> storage, float hourOfDay, WeatherSample weather, float hours)
        {
            var result = new TickResult { DemandMW = demandMW, HourOfDay = hourOfDay };

            foreach (IGenerator generator in generators)
            {
                float output = generator.Generate(hourOfDay, weather);
                if (generator.Kind == GeneratorKind.Wind) result.WindMW += output;
                else result.SolarMW += output;
            }

            foreach (IStorage store in storage) store.BeginTick();

            float generation = result.GenerationMW;
            if (generation >= demandMW)
            {
                result.DeliveredMW = demandMW;
                float surplus = generation - demandMW;
                result.ChargedMW = Share(storage, surplus, hours, true);
                result.WastedMW = surplus - result.ChargedMW;
            }
            else
            {
                float shortfall = demandMW - generation;
                result.DischargedMW = Share(storage, shortfall, hours, false);
                result.DeliveredMW = generation + result.DischargedMW;
                result.UnmetMW = demandMW - result.DeliveredMW;
            }

            foreach (IStorage store in storage)
            {
                result.StoredMWh += store.StoredMWh;
                result.CapacityMWh += store.CapacityMWh;
            }

            return result;
        }

        private static float Share(IReadOnlyList<IStorage> storage, float amountMW, float hours, bool charging)
        {
            float moved = 0f;
            for (int i = 0; i < storage.Count && amountMW - moved > 0.0001f; i++)
            {
                int remaining = storage.Count - i;
                float slice = (amountMW - moved) / remaining;
                moved += charging ? storage[i].Charge(slice, hours) : storage[i].Discharge(slice, hours);
            }
            return moved;
        }
    }
}
