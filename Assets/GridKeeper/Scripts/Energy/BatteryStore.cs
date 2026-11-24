using UnityEngine;

namespace GridKeeper.Energy
{
    public sealed class BatteryStore : IStorage
    {
        public float CapacityMWh { get; }
        public float MaxRateMW { get; }
        public float StoredMWh { get; private set; }
        public float RateLeftMW { get; private set; }
        public float Charge01 => CapacityMWh <= 0f ? 0f : StoredMWh / CapacityMWh;
        public float RoomMWh => Mathf.Max(0f, CapacityMWh - StoredMWh);

        public BatteryStore(float capacityMWh, float maxRateMW, float storedMWh = 0f)
        {
            CapacityMWh = Mathf.Max(0f, capacityMWh);
            MaxRateMW = Mathf.Max(0f, maxRateMW);
            StoredMWh = Mathf.Clamp(storedMWh, 0f, CapacityMWh);
            RateLeftMW = MaxRateMW;
        }

        public void BeginTick()
        {
            RateLeftMW = MaxRateMW;
        }

        public float Charge(float surplusMW, float hours)
        {
            if (hours <= 0f) return 0f;
            float mw = Mathf.Max(0f, Mathf.Min(surplusMW, RateLeftMW, RoomMWh / hours));
            StoredMWh = Mathf.Min(CapacityMWh, StoredMWh + mw * hours);
            RateLeftMW -= mw;
            return mw;
        }

        public float Discharge(float shortfallMW, float hours)
        {
            if (hours <= 0f) return 0f;
            float mw = Mathf.Max(0f, Mathf.Min(shortfallMW, RateLeftMW, StoredMWh / hours));
            StoredMWh = Mathf.Max(0f, StoredMWh - mw * hours);
            RateLeftMW -= mw;
            return mw;
        }
    }
}
