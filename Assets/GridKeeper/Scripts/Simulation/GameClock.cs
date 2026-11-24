using UnityEngine;

namespace GridKeeper.Simulation
{
    public sealed class GameClock
    {
        public float StartHour { get; }
        public float TickHours { get; }
        public float HoursPerSecond { get; }
        public float ElapsedHours { get; private set; }

        public float HourOfDay => Mathf.Repeat(StartHour + ElapsedHours, 24f);
        public int Day => 1 + Mathf.FloorToInt(ElapsedHours / 24f);

        private float pendingHours;

        public GameClock(float startHour, float secondsPerDay, float tickHours)
        {
            StartHour = startHour;
            TickHours = tickHours;
            HoursPerSecond = 24f / Mathf.Max(1f, secondsPerDay);
        }

        public int Advance(float seconds)
        {
            pendingHours += seconds * HoursPerSecond;
            int ticks = 0;
            while (pendingHours >= TickHours)
            {
                pendingHours -= TickHours;
                ElapsedHours += TickHours;
                ticks++;
            }
            return ticks;
        }

        public float Interpolation => Mathf.Clamp01(pendingHours / TickHours);
        public float SmoothElapsedHours => ElapsedHours + pendingHours;
        public float SmoothHourOfDay => Mathf.Repeat(StartHour + SmoothElapsedHours, 24f);

        public void Reset()
        {
            ElapsedHours = 0f;
            pendingHours = 0f;
        }

        public static string Format(float hourOfDay)
        {
            int h = Mathf.FloorToInt(hourOfDay) % 24;
            int m = Mathf.FloorToInt((hourOfDay - Mathf.Floor(hourOfDay)) * 60f);
            return h.ToString("00") + ":" + m.ToString("00");
        }
    }
}
