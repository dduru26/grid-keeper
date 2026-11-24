using UnityEngine;

namespace GridKeeper.Energy
{
    public class SolarPanel : PowerSource
    {
        public const float Sunrise = 6f;
        public const float Sunset = 18f;

        public override GeneratorKind Kind => GeneratorKind.Solar;

        public static float SunFactor(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            if (hour <= Sunrise || hour >= Sunset) return 0f;
            return Mathf.Sin(Mathf.PI * (hour - Sunrise) / (Sunset - Sunrise));
        }

        protected override float OutputMW(float hour, WeatherSample weather)
        {
            float cloudLoss = Balance != null ? Balance.heavyCloudSolarLoss : 0.75f;
            float cloud = 1f - cloudLoss * Mathf.Clamp01(weather.cloudCover);
            return Definition.RatedMW * SunFactor(hour) * cloud;
        }
    }
}
