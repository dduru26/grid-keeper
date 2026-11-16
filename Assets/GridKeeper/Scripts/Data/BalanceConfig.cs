using UnityEngine;

namespace GridKeeper.Data
{
    [CreateAssetMenu(menuName = "Grid Keeper/Balance Config", fileName = "BalanceConfig")]
    public class BalanceConfig : ScriptableObject
    {
        [Header("Economy")]
        [Min(0)] public int startingBudget = 900;
        [Range(0f, 1f)] public float refundFraction = 0.5f;
        [Min(0f)] public float revenuePerMWh = 3f;

        [Header("Wind")]
        [Range(0f, 2f)] public float hillWindBonus = 0.4f;
        [Range(0f, 1f)] public float wakePenalty = 0.2f;

        [Header("Solar")]
        [Range(0f, 1f)] public float heavyCloudSolarLoss = 0.75f;

        [Header("Demand")]
        [Min(1f)] public float heatwaveDemandFactor = 1.3f;

        [Header("Table")]
        [Min(0.1f)] public float minCityScale = 0.5f;
        [Min(0.1f)] public float maxCityScale = 2.5f;
    }
}
