using UnityEngine;

namespace GridKeeper.Energy
{
    public class Battery : Placeable, IStorage
    {
        [SerializeField, Tooltip("Child scaled on Y to show the charge level.")]
        private Transform chargeBar;

        private BatteryStore store = new BatteryStore(0f, 0f);

        public float StoredMWh => store.StoredMWh;
        public float CapacityMWh => store.CapacityMWh;
        public float MaxRateMW => store.MaxRateMW;
        public float Charge01 => store.Charge01;

        protected override void OnPlaced()
        {
            store = new BatteryStore(Definition.CapacityMWh, Definition.MaxRateMW);
            RefreshBar();
        }

        public void BeginTick()
        {
            store.BeginTick();
        }

        public float Charge(float surplusMW, float hours)
        {
            float mw = store.Charge(surplusMW, hours);
            RefreshBar();
            return mw;
        }

        public float Discharge(float shortfallMW, float hours)
        {
            float mw = store.Discharge(shortfallMW, hours);
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
