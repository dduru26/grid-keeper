using System;

namespace GridKeeper.Energy
{
    [Serializable]
    public struct WeatherSample
    {
        public float cloudCover;
        public float windStrength;
        public bool heatwave;

        public WeatherSample(float cloud, float wind, bool isHeatwave)
        {
            cloudCover = cloud;
            windStrength = wind;
            heatwave = isHeatwave;
        }

        public static WeatherSample Clear => new WeatherSample(0f, 0.4f, false);
    }
}
