using GridKeeper.Data;
using UnityEngine;

namespace GridKeeper.Simulation
{
    public readonly struct DayOutcome
    {
        public readonly Grade Grade;
        public readonly float ShareMet;
        public readonly int Bonus;
        public readonly bool Strike;

        public DayOutcome(Grade grade, float shareMet, int bonus, bool strike)
        {
            Grade = grade;
            ShareMet = shareMet;
            Bonus = bonus;
            Strike = strike;
        }
    }

    public static class RunRules
    {
        public static float DemandMultiplier(BalanceConfig balance, int dayIndex, bool heatwave)
        {
            float growth = Mathf.Min(balance.maxDemandGrowth, 1f + balance.demandGrowthPerDay * Mathf.Max(0, dayIndex));
            return growth * (heatwave ? balance.heatwaveDemandFactor : 1f);
        }

        public static DayOutcome ScoreDay(BalanceConfig balance, float demandMWh, float deliveredMWh)
        {
            float share = demandMWh <= 0f ? 1f : Mathf.Clamp01(deliveredMWh / demandMWh);
            Grade grade = GradeTable.Evaluate(share, balance.gradeA, balance.gradeB, balance.gradeC);
            int bonus = grade == Grade.A ? balance.dayBonusA : grade == Grade.B ? balance.dayBonusB : grade == Grade.C ? balance.dayBonusC : 0;
            return new DayOutcome(grade, share, bonus, grade == Grade.D);
        }

        public static int StrikesLeft(BalanceConfig balance, int strikes)
        {
            return Mathf.Max(0, balance.strikesAllowed - strikes);
        }
    }
}
