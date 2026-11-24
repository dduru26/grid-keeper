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
        public const int MaxSamples = 96 * 7;

        public readonly List<TickResult> Samples = new List<TickResult>();
        public int Score { get; private set; }
        public int DaysCompleted { get; private set; }
        public int Strikes { get; private set; }
        public int GradeADays { get; private set; }

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
            if (Samples.Count > MaxSamples) Samples.RemoveRange(0, Samples.Count - MaxSamples);
            DemandMWh += result.DemandMW * hours;
            DeliveredMWh += result.DeliveredMW * hours;
            WastedMWh += result.WastedMW * hours;
            SolarMWh += result.SolarMW * hours;
            WindMWh += result.WindMW * hours;
            if (result.IsBlackout) BlackoutHours += hours;
            Earned += earned;
            PeakDemandMW = Mathf.Max(PeakDemandMW, result.DemandMW);
        }

        public void AddPoints(int points)
        {
            Score += points;
        }

        public void CompleteDay(Grade grade, bool strike)
        {
            DaysCompleted++;
            if (grade == Grade.A) GradeADays++;
            if (strike) Strikes++;
        }

        public void Reset()
        {
            Score = DaysCompleted = Strikes = GradeADays = 0;
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
                case Grade.A: return "Brightwater slept soundly night after night. Legendary work, Keeper.";
                case Grade.B: return "A few dark evenings, but the town is proud of how long you lasted.";
                case Grade.C: return "Plenty of long nights. Store more sun and build wind on the hills.";
                default: return "The town spent too long in the dark. Try again and beat your score.";
            }
        }
    }
}
