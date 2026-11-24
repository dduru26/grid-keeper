using UnityEngine;

namespace GridKeeper.Data
{
    [CreateAssetMenu(menuName = "Grid Keeper/Balance Config", fileName = "BalanceConfig")]
    public class BalanceConfig : ScriptableObject
    {
        [Header("Economy")]
        [Min(0)] public int startingBudget = 1300;
        [Range(0f, 1f)] public float refundFraction = 0.5f;
        [Min(0f)] public float revenuePerMWh = 3f;

        [Header("Shift")]
        [Min(5f)] public float secondsPerDay = 40f;
        [Range(0f, 23f)] public float startHour = 6f;
        [Range(0.05f, 1f)] public float tickHours = 0.25f;

        [Header("Grades (share of demand met)")]
        [Range(0f, 1f)] public float gradeA = 0.9f;
        [Range(0f, 1f)] public float gradeB = 0.75f;
        [Range(0f, 1f)] public float gradeC = 0.55f;

        [Header("Wind")]
        [Range(0f, 2f)] public float hillWindBonus = 0.4f;
        [Range(0f, 1f)] public float wakePenalty = 0.2f;

        [Header("Solar")]
        [Range(0f, 1f)] public float heavyCloudSolarLoss = 0.75f;

        [Header("Demand")]
        [Min(1f)] public float heatwaveDemandFactor = 1.3f;
        [Range(0f, 0.2f)] public float demandGrowthPerDay = 0.04f;
        [Min(1f)] public float maxDemandGrowth = 2f;

        [Header("Endless run")]
        [Min(1)] public int strikesAllowed = 3;
        [Min(0f)] public float pointsPerMWh = 10f;
        [Min(0)] public int dayBonusA = 300;
        [Min(0)] public int dayBonusB = 150;
        [Min(0)] public int dayBonusC = 50;

        [Header("Table")]
        [Min(0.1f)] public float minCityScale = 0.5f;
        [Min(0.1f)] public float maxCityScale = 2.5f;
    }
}
