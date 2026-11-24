using GridKeeper.Construction;
using GridKeeper.Core;
using UnityEngine;

namespace GridKeeper.Presentation
{
    public class AudioDirector : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private BuildToolbar toolbar;
        [SerializeField] private DayNightCycle dayNight;

        [Header("Loops")]
        [SerializeField] private AudioClip ambientDay;
        [SerializeField] private AudioClip ambientNight;
        [SerializeField] private AudioClip windLoop;

        [Header("One shots")]
        [SerializeField] private AudioClip buildClick;
        [SerializeField] private AudioClip selectTick;
        [SerializeField] private AudioClip errorBuzz;
        [SerializeField] private AudioClip dawnChime;
        [SerializeField] private AudioClip duskPad;
        [SerializeField] private AudioClip blackout;
        [SerializeField] private AudioClip restored;
        [SerializeField] private AudioClip resultsChord;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float windVolume = 0.3f;
        [SerializeField, Range(0f, 1f)] private float effectsVolume = 0.8f;

        private AudioSource daySource;
        private AudioSource nightSource;
        private AudioSource windSource;
        private AudioSource effects;
        private float lastHour = -1f;
        private float windTarget;

        private void Awake()
        {
            Require.Reference(this, game, nameof(game));
            Require.Reference(this, toolbar, nameof(toolbar));
            Require.Reference(this, dayNight, nameof(dayNight));

            daySource = CreateLoop("Ambient Day", ambientDay);
            nightSource = CreateLoop("Ambient Night", ambientNight);
            windSource = CreateLoop("Wind", windLoop);
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            game.PhaseChanged += OnPhase;
            game.BlackoutChanged += OnBlackout;
            game.ShiftEnded += OnShiftEnded;
            game.Ticked += OnTick;
            toolbar.Built += OnBuilt;
            toolbar.Removed += OnBuilt;
            toolbar.ToolChanged += OnToolChanged;
            toolbar.Feedback += OnFeedback;
        }

        private void OnDisable()
        {
            game.PhaseChanged -= OnPhase;
            game.BlackoutChanged -= OnBlackout;
            game.ShiftEnded -= OnShiftEnded;
            game.Ticked -= OnTick;
            toolbar.Built -= OnBuilt;
            toolbar.Removed -= OnBuilt;
            toolbar.ToolChanged -= OnToolChanged;
            toolbar.Feedback -= OnFeedback;
        }

        private AudioSource CreateLoop(string name, AudioClip clip)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            source.spatialBlend = 0f;
            if (clip != null) source.Play();
            return source;
        }

        private void Update()
        {
            bool active = game.Phase == GamePhase.Playing || game.Phase == GamePhase.Briefing;
            float night = dayNight != null ? dayNight.Night01 : 0f;
            float level = active ? ambientVolume : ambientVolume * 0.4f;

            daySource.volume = Mathf.MoveTowards(daySource.volume, level * (1f - night), Time.deltaTime * 0.5f);
            nightSource.volume = Mathf.MoveTowards(nightSource.volume, level * night, Time.deltaTime * 0.5f);
            float wind = game.Phase == GamePhase.Playing ? windTarget : 0.15f;
            windSource.volume = Mathf.MoveTowards(windSource.volume, windVolume * wind, Time.deltaTime * 0.4f);
            windSource.pitch = 0.8f + wind * 0.4f;
        }

        private void OnTick(Simulation.TickResult tick)
        {
            windTarget = game.Weather.Current.windStrength;

            float hour = tick.HourOfDay;
            if (lastHour >= 0f && game.Phase == GamePhase.Playing)
            {
                if (Crossed(lastHour, hour, 6f)) Play(dawnChime);
                if (Crossed(lastHour, hour, 18f)) Play(duskPad);
            }
            lastHour = hour;
        }

        private static bool Crossed(float from, float to, float mark)
        {
            if (to >= from) return from < mark && to >= mark;
            return from < mark || to >= mark;
        }

        private void OnPhase(GamePhase phase)
        {
            if (phase == GamePhase.Briefing) lastHour = -1f;
        }

        private void OnBlackout(bool dark)
        {
            if (game.Phase != GamePhase.Playing) return;
            Play(dark ? blackout : restored);
        }

        private void OnShiftEnded(ShiftResult result)
        {
            Play(resultsChord);
        }

        private void OnBuilt(World.Tile tile)
        {
            Play(buildClick);
        }

        private void OnToolChanged()
        {
            Play(selectTick, 0.5f);
        }

        private void OnFeedback(BuildFeedback feedback)
        {
            if (!feedback.Positive) Play(errorBuzz, 0.6f);
        }

        private void Play(AudioClip clip, float volume = 1f)
        {
            if (clip != null) effects.PlayOneShot(clip, effectsVolume * volume);
        }
    }
}
