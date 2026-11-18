using System.Collections.Generic;
using GridKeeper.Energy;

namespace GridKeeper.Simulation
{
    public sealed class EnergyGrid
    {
        private readonly List<Battery> batteries = new List<Battery>();

        public TickResult Balance(float demandMW, IReadOnlyList<Placeable> placeables, float hourOfDay, WeatherSample weather, float hours)
        {
            var result = new TickResult { DemandMW = demandMW, HourOfDay = hourOfDay };
            batteries.Clear();

            foreach (Placeable placeable in placeables)
            {
                switch (placeable)
                {
                    case WindTurbine turbine:
                        result.WindMW += turbine.Generate(hourOfDay, weather);
                        break;
                    case PowerSource source:
                        result.SolarMW += source.Generate(hourOfDay, weather);
                        break;
                    case Battery battery:
                        batteries.Add(battery);
                        break;
                }
            }

            float generation = result.GenerationMW;
            if (generation >= demandMW)
            {
                result.DeliveredMW = demandMW;
                float surplus = generation - demandMW;
                result.ChargedMW = Share(surplus, hours, true);
                result.WastedMW = surplus - result.ChargedMW;
            }
            else
            {
                float shortfall = demandMW - generation;
                result.DischargedMW = Share(shortfall, hours, false);
                result.DeliveredMW = generation + result.DischargedMW;
                result.UnmetMW = demandMW - result.DeliveredMW;
            }

            foreach (Battery battery in batteries)
            {
                result.StoredMWh += battery.StoredMWh;
                result.CapacityMWh += battery.CapacityMWh;
            }

            return result;
        }

        private float Share(float amountMW, float hours, bool charging)
        {
            float moved = 0f;
            for (int i = 0; i < batteries.Count && amountMW - moved > 0.0001f; i++)
            {
                int remaining = batteries.Count - i;
                float slice = (amountMW - moved) / remaining;
                moved += charging ? batteries[i].Charge(slice, hours) : batteries[i].Discharge(slice, hours);
            }


            return moved;
        }
    }
}
