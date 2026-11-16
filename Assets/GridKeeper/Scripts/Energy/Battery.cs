using UnityEngine;

namespace GridKeeper.Energy
{
    public class Battery : Placeable
    {
        [SerializeField, Tooltip("Child scaled on Y to show the charge level.")]
        private Transform chargeBar;

        public float StoredMWh { get; private set; }
        public float CapacityMWh => Definition != null ? Definition.CapacityMWh : 0f;
        public float MaxRateMW => Definition != null ? Definition.MaxRateMW : 0f;
        public float Charge01 => CapacityMWh <= 0f ? 0f : StoredMWh / CapacityMWh;
        public float RoomMWh => Mathf.Max(0f, CapacityMWh - StoredMWh);

        protected override void OnPlaced()
        {
            StoredMWh = 0f;
            RefreshBar();
        }

        public float Charge(float surplusMW, float hours)
        {
            if (hours <= 0f) return 0f;
            float mw = Mathf.Min(surplusMW, MaxRateMW, RoomMWh / hours);
            mw = Mathf.Max(0f, mw);
            StoredMWh += mw * hours;
            RefreshBar();
            return mw;
        }

        public float Discharge(float shortfallMW, float hours)
        {
            if (hours <= 0f) return 0f;
            float mw = Mathf.Min(shortfallMW, MaxRateMW, StoredMWh / hours);
            mw = Mathf.Max(0f, mw);
            StoredMWh -= mw * hours;
            RefreshBar();
            return mw;
        }

        private void RefreshBar()
        {
            if (chargeBar == null) return;
            Vector3 scale = chargeBar.localScale;
            scale.y = Mathf.Max(0.001f, Charge01);
            chargeBar.localScale = scale;
        }
    }
}
