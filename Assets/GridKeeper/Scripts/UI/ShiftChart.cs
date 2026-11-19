using System.Collections.Generic;
using GridKeeper.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridKeeper.UI
{
    public class ShiftChart : VisualElement
    {
        public static readonly Color Solar = new Color32(0xF2, 0xA5, 0x16, 0xFF);
        public static readonly Color Wind = new Color32(0x10, 0xA0, 0xA3, 0xFF);
        public static readonly Color Battery = new Color32(0x7B, 0x5C, 0xE6, 0xFF);
        public static readonly Color Shortfall = new Color32(0xD6, 0x45, 0x3B, 0xFF);
        private static readonly Color Demand = Color.white;
        private static readonly Color GridLine = new Color(1f, 1f, 1f, 0.12f);

        private IReadOnlyList<TickResult> samples;
        private float windowHours = 72f;
        private float totalHours = 72f;
        private bool follow;
        private float scaleMW = 20f;

        public ShiftChart()
        {
            generateVisualContent += Draw;
            pickingMode = PickingMode.Ignore;
        }

        public void SetData(IReadOnlyList<TickResult> data, float window, float total, bool followLatest)
        {
            samples = data;
            windowHours = Mathf.Max(1f, window);
            totalHours = Mathf.Max(1f, total);
            follow = followLatest;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            Rect r = contentRect;
            if (r.width < 4f || r.height < 4f) return;
            Painter2D p = context.painter2D;

            p.strokeColor = GridLine;
            p.lineWidth = 1f;
            for (int i = 1; i < 4; i++)
            {
                float y = r.yMin + r.height * i / 4f;
                p.BeginPath();
                p.MoveTo(new Vector2(r.xMin, y));
                p.LineTo(new Vector2(r.xMax, y));
                p.Stroke();
            }

            if (samples == null || samples.Count < 2) return;

            float maxMW = 1f;
            foreach (TickResult s in samples) maxMW = Mathf.Max(maxMW, s.DemandMW, s.GenerationMW);
            scaleMW = Mathf.Lerp(scaleMW, Mathf.Ceil(maxMW / 5f) * 5f, 0.25f);

            float end = follow ? Mathf.Max(windowHours, samples[samples.Count - 1].ElapsedHours) : totalHours;
            float start = end - windowHours;
            int first = 0;
            while (first < samples.Count - 1 && samples[first].ElapsedHours < start) first++;

            float X(float hours) => r.xMin + (hours - start) / windowHours * r.width;
            float Y(float mw) => r.yMax - Mathf.Clamp01(mw / scaleMW) * r.height;

            for (int i = first; i < samples.Count; i++)
            {
                if (!samples[i].IsBlackout) continue;
                float x0 = X(samples[i].ElapsedHours);
                float x1 = X(samples[i].ElapsedHours + 0.25f);
                p.fillColor = new Color(Shortfall.r, Shortfall.g, Shortfall.b, 0.28f);
                p.BeginPath();
                p.MoveTo(new Vector2(x0, r.yMin));
                p.LineTo(new Vector2(x1, r.yMin));
                p.LineTo(new Vector2(x1, r.yMax));
                p.LineTo(new Vector2(x0, r.yMax));
                p.ClosePath();
                p.Fill();
            }

            FillArea(p, first, X, Y, s => s.SolarMW + s.WindMW, s => s.SolarMW, Wind);
            FillArea(p, first, X, Y, s => s.SolarMW, s => 0f, Solar);

            p.strokeColor = Battery;
            p.lineWidth = 3f;
            p.BeginPath();
            for (int i = first; i < samples.Count; i++)
            {
                Vector2 point = new Vector2(X(samples[i].ElapsedHours), r.yMax - samples[i].Charge01 * r.height);
                if (i == first) p.MoveTo(point); else p.LineTo(point);
            }
            p.Stroke();

            p.strokeColor = Demand;
            p.lineWidth = 3f;
            p.BeginPath();
            for (int i = first; i < samples.Count; i++)
            {
                Vector2 point = new Vector2(X(samples[i].ElapsedHours), Y(samples[i].DemandMW));
                if (i == first) p.MoveTo(point); else p.LineTo(point);
            }
            p.Stroke();
        }

        private void FillArea(Painter2D p, int first, System.Func<float, float> x, System.Func<float, float> y,
            System.Func<TickResult, float> top, System.Func<TickResult, float> bottom, Color color)
        {
            p.fillColor = new Color(color.r, color.g, color.b, 0.85f);
            p.BeginPath();
            p.MoveTo(new Vector2(x(samples[first].ElapsedHours), y(bottom(samples[first]))));
            for (int i = first; i < samples.Count; i++) p.LineTo(new Vector2(x(samples[i].ElapsedHours), y(top(samples[i]))));
            for (int i = samples.Count - 1; i >= first; i--) p.LineTo(new Vector2(x(samples[i].ElapsedHours), y(bottom(samples[i]))));
            p.ClosePath();
            p.Fill();
        }
    }
}
