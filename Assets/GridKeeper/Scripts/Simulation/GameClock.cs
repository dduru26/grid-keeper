using UnityEngine;

namespace GridKeeper.Simulation
{
    public sealed class GameClock
    {
        public float StartHour { get; }
        public int Days { get; }
        public float TickHours { get; }
        public float HoursPerSecond { get; }
        public float ElapsedHours { get; private set; }

        public float TotalHours => Days * 24f;
        public float HourOfDay => Mathf.Repeat(StartHour + ElapsedHours, 24f);
        public int Day => Mathf.Min(Days, 1 + Mathf.FloorToInt(ElapsedHours / 24f));
        public float Progress01 => Mathf.Clamp01(ElapsedHours / TotalHours);
        public bool IsFinished => ElapsedHours >= TotalHours - 0.0001f;

        private float pendingHours;

        public GameClock(float startHour, int days, float shiftSeconds, float tickHours)
        {
            StartHour = startHour;
            Days = days;
            TickHours = tickHours;
            HoursPerSecond = days * 24f / Mathf.Max(1f, shiftSeconds);
        }

        public int Advance(float seconds)
        {
            pendingHours += seconds * HoursPerSecond;
            int ticks = 0;
            while (pendingHours >= TickHours && !IsFinished)
            {
                pendingHours -= TickHours;
                ElapsedHours += TickHours;
                ticks++;
            }
            return ticks;
        }

        public float Interpolation => Mathf.Clamp01(pendingHours / TickHours);

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
