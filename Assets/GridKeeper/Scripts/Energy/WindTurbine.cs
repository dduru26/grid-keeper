using GridKeeper.World;
using UnityEngine;

namespace GridKeeper.Energy
{
    public class WindTurbine : PowerSource
    {
        [SerializeField] private Transform rotor;
        [SerializeField, Tooltip("Rotor speed in degrees per second at full wind.")]
        private float maxSpinSpeed = 540f;

        private float windStrength = 0.4f;

        public override GeneratorKind Kind => GeneratorKind.Wind;

        public bool HasWakePenalty
        {
            get
            {
                if (Tile == null || Tile.City == null) return false;
                foreach (Tile neighbour in Tile.City.Neighbours(Tile))
                {
                    if (neighbour.Occupant is WindTurbine) return true;
                }
                return false;
            }
        }

        public float SiteMultiplier
        {
            get
            {
                float hill = Balance != null ? Balance.hillWindBonus : 0.4f;
                float wake = Balance != null ? Balance.wakePenalty : 0.2f;
                float multiplier = 1f;
                if (Tile != null && Tile.IsHill) multiplier += hill;
                if (HasWakePenalty) multiplier *= 1f - wake;
                return multiplier;
            }
        }

        protected override float OutputMW(float hour, WeatherSample weather)
        {
            windStrength = Mathf.Clamp01(weather.windStrength);
            return Definition.RatedMW * windStrength * SiteMultiplier;
        }

        public void SetWind(float strength)
        {
            windStrength = Mathf.Clamp01(strength);
        }

        private void Update()
        {
            if (rotor == null) return;
            rotor.Rotate(Vector3.forward, windStrength * maxSpinSpeed * Time.deltaTime, Space.Self);
        }
    }
}
