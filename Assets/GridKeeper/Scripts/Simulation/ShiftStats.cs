using System.Collections.Generic;
using UnityEngine;

namespace GridKeeper.Simulation
{
    public enum Grade
    {
        A,
        B,
        C,
        D
    }

    public sealed class ShiftStats
    {
        public readonly List<TickResult> Samples = new List<TickResult>();

        public float DemandMWh { get; private set; }
        public float DeliveredMWh { get; private set; }
        public float WastedMWh { get; private set; }
        public float SolarMWh { get; private set; }
        public float WindMWh { get; private set; }
        public float BlackoutHours { get; private set; }
        public float Earned { get; private set; }
        public float PeakDemandMW { get; private set; }

        public float ShareMet => DemandMWh <= 0f ? 1f : Mathf.Clamp01(DeliveredMWh / DemandMWh);

        public void Record(TickResult result, float hours, float earned)
        {
            Samples.Add(result);
            DemandMWh += result.DemandMW * hours;
            DeliveredMWh += result.DeliveredMW * hours;
            WastedMWh += result.WastedMW * hours;
            SolarMWh += result.SolarMW * hours;
            WindMWh += result.WindMW * hours;
            if (result.IsBlackout) BlackoutHours += hours;
            Earned += earned;
            PeakDemandMW = Mathf.Max(PeakDemandMW, result.DemandMW);
        }

        public void Reset()
        {
            Samples.Clear();
            DemandMWh = DeliveredMWh = WastedMWh = SolarMWh = WindMWh = BlackoutHours = Earned = PeakDemandMW = 0f;
        }
    }

    public static class GradeTable
    {
        public static Grade Evaluate(float shareMet, float a, float b, float c)
        {
            if (shareMet >= a) return Grade.A;
            if (shareMet >= b) return Grade.B;
            if (shareMet >= c) return Grade.C;
            return Grade.D;
        }

        public static string CouncilNote(Grade grade)
        {
            switch (grade)
            {
                case Grade.A: return "Brightwater slept soundly. Please stay on as our Keeper.";
                case Grade.B: return "A few dark evenings, but the town is proud of you.";
                case Grade.C: return "We made it through, with some long nights. Try storing more sun.";
                default: return "The town spent too long in the dark. Let's try again.";
            }
        }
    }
}
