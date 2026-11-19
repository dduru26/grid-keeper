using GridKeeper.Core;
using GridKeeper.Data;
using GridKeeper.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridKeeper.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class OverlayView : MonoBehaviour
    {
        [SerializeField] private GameManager game;

        private VisualElement briefing;
        private VisualElement pause;
        private VisualElement results;
        private VisualElement briefingForecast;
        private VisualElement buildLayer;
        private Label grade;
        private Label share;
        private Label note;
        private Label delivered;
        private Label blackout;
        private Label wasted;
        private Label earned;
        private ShiftChart chart;
        private Button startButton;
        private bool briefingDismissed;

        private void Start()
        {
            if (game == null) game = FindAnyObjectByType<GameManager>();
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            briefing = root.Q<VisualElement>("briefing");
            pause = root.Q<VisualElement>("pause");
            results = root.Q<VisualElement>("results");
            briefingForecast = root.Q<VisualElement>("briefing-forecast");
            buildLayer = root.Q<VisualElement>("build-layer");
            grade = root.Q<Label>("results-grade");
            share = root.Q<Label>("results-share");
            note = root.Q<Label>("results-note");
            delivered = root.Q<Label>("results-delivered");
            blackout = root.Q<Label>("results-blackout");
            wasted = root.Q<Label>("results-wasted");
            earned = root.Q<Label>("results-earned");

            chart = new ShiftChart();
            chart.style.flexGrow = 1;
            root.Q<VisualElement>("results-chart").Add(chart);

            startButton = root.Q<Button>("start-button");
            startButton.clicked += game.StartShift;
            root.Q<Button>("plan-button").clicked += DismissBriefing;
            root.Q<Button>("resume-button").clicked += game.TogglePause;
            root.Q<Button>("restart-button").clicked += game.Restart;
            root.Q<Button>("again-button").clicked += game.Restart;

            game.PhaseChanged += Render;
            game.ShiftEnded += ShowResults;
            Render(game.Phase);
        }

        private void OnDestroy()
        {
            if (game == null) return;
            game.PhaseChanged -= Render;
            game.ShiftEnded -= ShowResults;
        }

        private void DismissBriefing()
        {
            briefingDismissed = true;
            Render(game.Phase);
        }

        private void Render(GamePhase phase)
        {
            if (phase != GamePhase.Briefing) briefingDismissed = false;
            bool planning = phase == GamePhase.Briefing && briefingDismissed;
            briefing.EnableInClassList("hidden", phase != GamePhase.Briefing || briefingDismissed);
            startButton.EnableInClassList("hidden", !planning);
            pause.EnableInClassList("hidden", phase != GamePhase.Paused);
            results.EnableInClassList("hidden", phase != GamePhase.Results);
            buildLayer.EnableInClassList("hidden", phase == GamePhase.Placing || phase == GamePhase.Results || (phase == GamePhase.Briefing && !briefingDismissed));

            if (phase == GamePhase.Briefing && !briefingDismissed) RenderBriefingForecast();
        }

        private void RenderBriefingForecast()
        {
            briefingForecast.Clear();
            for (int i = 0; i < game.Weather.Forecast.Count; i++)
            {
                WeatherProfile profile = game.Weather.Forecast[i];
                var chip = new VisualElement();
                chip.AddToClassList("forecast__chip");
                chip.style.marginBottom = 8;
                var swatch = new VisualElement();
                swatch.AddToClassList("forecast__swatch");
                swatch.style.backgroundColor = profile.Tint;
                var label = new Label("Day " + (i + 1) + ": " + profile.Label);
                label.AddToClassList("forecast__text");
                chip.Add(swatch);
                chip.Add(label);
                briefingForecast.Add(chip);
            }
        }

        private void ShowResults(ShiftResult result)
        {
            ShiftStats stats = result.Stats;
            grade.text = result.Grade.ToString();
            grade.EnableInClassList("grade--D", result.Grade == Grade.D);
            share.text = Mathf.RoundToInt(result.ShareMet * 100f) + "% of demand met";
            note.text = result.CouncilNote;
            delivered.text = "Energy delivered    " + stats.DeliveredMWh.ToString("0") + " of " + stats.DemandMWh.ToString("0") + " MWh";
            blackout.text = "Hours in the dark   " + stats.BlackoutHours.ToString("0.0") + " h";
            wasted.text = "Energy wasted       " + stats.WastedMWh.ToString("0") + " MWh";
            earned.text = "Money earned        $" + stats.Earned.ToString("N0");
            chart.SetData(stats.Samples, game.Clock.TotalHours, game.Clock.TotalHours, false);
        }
    }
}
