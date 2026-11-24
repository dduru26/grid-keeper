using GridKeeper.Core;
using GridKeeper.Data;
using GridKeeper.Simulation;
using GridKeeper.Tutorial;
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
        private Label briefingEyebrow;
        private Label briefingTitle;
        private Label briefingBody;
        private VisualElement briefingVisual;
        private VisualElement briefingDots;
        private Button planButton;
        private Button skipButton;
        private int card;
        private bool storyMode;
        private bool briefingShown;
        private string bestLine;
        private Label resultsEyebrow;
        private Label resultsScore;
        private Label resultsBest;
        private Label resultsBestCaption;
        private Label resultsDays;

        private static readonly Color Solar = new Color(0.949f, 0.647f, 0.086f);
        private static readonly Color Wind = new Color(0.063f, 0.627f, 0.639f);
        private static readonly Color Stored = new Color(0.545f, 0.42f, 0.886f);
        private static readonly Color Demand = Color.white;
        private static readonly Color Dark = new Color(0.84f, 0.27f, 0.23f);

        private struct StoryCard
        {
            public string Eyebrow;
            public string Title;
            public string Body;
            public (Color colour, string text, bool line)[] Rows;
        }

        private static readonly StoryCard[] Story =
        {
            new StoryCard
            {
                Eyebrow = "THE STORY",
                Title = "Brightwater needs you.",
                Body = "The town's old coal generator has shut down for good. From today, every light in Brightwater runs on clean energy that you build."
            },
            new StoryCard
            {
                Eyebrow = "YOUR GOAL",
                Title = "Keep the lights on as long as you can.",
                Body = "The days never stop, and the town needs a little more power every day. Every hour you power earns points.",
                Rows = new[]
                {
                    (new Color(0.36f, 0.75f, 0.45f), "Meet 90% of a day's need for a big bonus", false),
                    (Dark, "A day mostly in the dark costs a life. Lose 3 and the run ends", false)
                }
            },
            new StoryCard
            {
                Eyebrow = "YOUR TOOLS",
                Title = "Three ways to power the town.",
                Body = "Tap a tool, then tap an empty grass tile to build it.",
                Rows = new[]
                {
                    (Solar, "Solar makes power while the sun is up", false),
                    (Wind, "Wind works day and night. Best on green hills", false),
                    (Stored, "Batteries save spare power for the night", false)
                }
            },
            new StoryCard
            {
                Eyebrow = "READ THE CHART",
                Title = "Colours over the white line.",
                Body = "The chart shows what the town needs and what you give it. Keep the colours at or above the line.",
                Rows = new[]
                {
                    (Demand, "White line: what the town needs", true),
                    (Solar, "Colours: the power you supply", false),
                    (Dark, "Red edge on screen: a blackout", false)
                }
            },
            new StoryCard
            {
                Eyebrow = "WATCH THE SKY",
                Title = "The weather changes every day.",
                Body = "Every day gets random weather. Clouds cut solar, wind comes and goes, and a heatwave makes the town hungrier. Check tomorrow's forecast and plan ahead."
            }
        };

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
            briefingEyebrow = root.Q<Label>("briefing-eyebrow");
            briefingTitle = root.Q<Label>("briefing-title");
            briefingBody = root.Q<Label>("briefing-body");
            briefingVisual = root.Q<VisualElement>("briefing-visual");
            briefingDots = root.Q<VisualElement>("briefing-dots");
            planButton = root.Q<Button>("plan-button");
            skipButton = root.Q<Button>("briefing-skip");
            planButton.clicked += NextCard;
            skipButton.clicked += SkipStory;
            root.Q<Button>("resume-button").clicked += game.TogglePause;
            root.Q<Button>("restart-button").clicked += game.Restart;
            root.Q<Button>("end-run-button").clicked += game.EndRun;
            resultsEyebrow = root.Q<Label>("results-eyebrow");
            resultsScore = root.Q<Label>("results-score");
            resultsBest = root.Q<Label>("results-best");
            resultsBestCaption = root.Q<Label>("results-best-caption");
            resultsDays = root.Q<Label>("results-days");
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

        private void NextCard()
        {
            if (storyMode && card < Story.Length)
            {
                card++;
                RenderCard();
                return;
            }
            DismissBriefing();
        }

        private void SkipStory()
        {
            card = Story.Length;
            RenderCard();
        }

        private void RenderCard()
        {
            bool final = !storyMode || card >= Story.Length;
            briefingVisual.Clear();
            briefingDots.Clear();
            skipButton.EnableInClassList("hidden", final);
            briefingDots.EnableInClassList("hidden", !storyMode);
            briefingForecast.EnableInClassList("hidden", !final);

            if (storyMode)
            {
                for (int i = 0; i <= Story.Length; i++)
                {
                    var dot = new VisualElement();
                    dot.AddToClassList("story__dot");
                    dot.EnableInClassList("story__dot--on", i == card);
                    briefingDots.Add(dot);
                }
            }

            if (final)
            {
                briefingEyebrow.text = "A MESSAGE FROM THE COUNCIL";
                briefingTitle.text = "Welcome, Keeper.";
                briefingBody.text = "Houses need light at night, the factory runs in working hours and the hospital can never go dark. Here is the forecast for today and tomorrow. How long can you last?";
                planButton.text = "Plan the grid";
                bestLine = GameManager.BestScore > 0 ? " Your best score is " + GameManager.BestScore.ToString("N0") + "." : string.Empty;
                briefingBody.text += bestLine;
                RenderBriefingForecast();
                return;
            }

            StoryCard current = Story[card];
            briefingEyebrow.text = current.Eyebrow;
            briefingTitle.text = current.Title;
            briefingBody.text = current.Body;
            planButton.text = "Next";
            if (current.Rows == null) return;
            foreach (var row in current.Rows)
            {
                var item = new VisualElement();
                item.AddToClassList("story__row");
                var swatch = new VisualElement();
                swatch.AddToClassList("story__swatch");
                swatch.EnableInClassList("story__swatch--line", row.line);
                swatch.style.backgroundColor = row.colour;
                var label = new Label(row.text);
                label.AddToClassList("story__row-text");
                item.Add(swatch);
                item.Add(label);
                briefingVisual.Add(item);
            }
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

            if (phase == GamePhase.Briefing && !briefingDismissed && !briefingShown)
            {
                briefingShown = true;
                storyMode = !TutorialDirector.Completed;
                card = 0;
                RenderCard();
            }
            if (phase != GamePhase.Briefing) briefingShown = false;
        }

        private void RenderBriefingForecast()
        {
            briefingForecast.Clear();
            for (int i = 0; i < 2; i++)
            {
                WeatherProfile profile = game.Weather.ForDay(i);
                var chip = new VisualElement();
                chip.AddToClassList("forecast__chip");
                chip.style.marginBottom = 8;
                var swatch = new VisualElement();
                swatch.AddToClassList("forecast__swatch");
                swatch.style.backgroundColor = profile.Tint;
                var label = new Label((i == 0 ? "Today: " : "Tomorrow: ") + profile.Label);
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
            int days = stats.DaysCompleted;
            resultsEyebrow.text = game.StrikesLeft <= 0 ? "THE LIGHTS WENT OUT" : "RUN ENDED";
            resultsScore.text = stats.Score.ToString("N0");
            resultsBest.text = result.BestScore.ToString("N0");
            resultsBestCaption.text = result.NewBest ? "NEW BEST" : "BEST";
            resultsBestCaption.EnableInClassList("results__score-caption--new", result.NewBest);
            resultsDays.text = days + (days == 1 ? " day" : " days");
            note.text = result.CouncilNote;
            delivered.text = stats.DeliveredMWh.ToString("0") + " of " + stats.DemandMWh.ToString("0") + " MWh";
            blackout.text = stats.BlackoutHours.ToString("0.0") + " h";
            wasted.text = stats.WastedMWh.ToString("0") + " MWh";
            earned.text = "$" + stats.Earned.ToString("N0");
            float shown = Mathf.Clamp(game.Clock.ElapsedHours, 24f, 72f);
            chart.SetData(stats.Samples, shown, game.Clock.ElapsedHours, true);
        }
    }
}
