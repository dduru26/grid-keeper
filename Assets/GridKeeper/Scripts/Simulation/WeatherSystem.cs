using System;
using System.Collections.Generic;
using GridKeeper.Data;
using GridKeeper.Energy;
using UnityEngine;

namespace GridKeeper.Simulation
{
    public class WeatherSystem : MonoBehaviour
    {
        [SerializeField] private WeatherProfile clear;
        [SerializeField] private WeatherProfile cloudy;
        [SerializeField] private WeatherProfile windy;
        [SerializeField] private WeatherProfile heatwave;
        [SerializeField, Tooltip("0 picks a new forecast each shift.")]
        private int fixedSeed;

        public event Action<int, WeatherProfile> DayStarted;

        public IReadOnlyList<WeatherProfile> Forecast => forecast;
        public WeatherSample Current { get; private set; }

        private readonly List<WeatherProfile> forecast = new List<WeatherProfile>();
        private System.Random random;
        private float noiseOffset;
        private int currentDay = -1;

        public void Generate(int days)
        {
            int seed = fixedSeed != 0 ? fixedSeed : Environment.TickCount;
            random = new System.Random(seed);
            noiseOffset = (float)random.NextDouble() * 100f;
            currentDay = -1;

            forecast.Clear();
            for (int day = 0; day < days; day++)
            {
                forecast.Add(PickFor(day, days));
            }
        }

        private WeatherProfile PickFor(int day, int days)
        {
            if (day == 0) return clear;
            if (day == days - 1) return random.NextDouble() < 0.7 ? heatwave : windy;
            return random.NextDouble() < 0.5 ? cloudy : windy;
        }

        public WeatherProfile ForDay(int dayIndex)
        {
            if (forecast.Count == 0) return clear;
            return forecast[Mathf.Clamp(dayIndex, 0, forecast.Count - 1)];
        }

        public WeatherSample Sample(float elapsedHours)
        {
            int dayIndex = Mathf.FloorToInt(elapsedHours / 24f);
            WeatherProfile profile = ForDay(dayIndex);

            if (dayIndex != currentDay && dayIndex < forecast.Count)
            {
                currentDay = dayIndex;
                DayStarted?.Invoke(dayIndex + 1, profile);
            }

            float cloudNoise = Mathf.PerlinNoise(noiseOffset + elapsedHours * 0.18f, 0.37f);
            float windNoise = Mathf.PerlinNoise(noiseOffset * 0.5f + elapsedHours * 0.25f, 7.21f);
            float cloud = Mathf.Lerp(profile.CloudRange.x, profile.CloudRange.y, cloudNoise);
            float wind = Mathf.Lerp(profile.WindRange.x, profile.WindRange.y, windNoise);

            Current = new WeatherSample(cloud, wind, profile.Heatwave);
            return Current;
        }
    }
}
