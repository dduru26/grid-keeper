using GridKeeper.Core;
using UnityEngine;

namespace GridKeeper.Presentation
{
    public class CloudLayer : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private GameObject cloudPrefab;
        [SerializeField, Min(1)] private int count = 5;
        [SerializeField] private float height = 0.13f;
        [SerializeField] private float halfWidth = 0.26f;
        [SerializeField] private float driftSpeed = 0.02f;
        [SerializeField] private float cloudScale = 1.8f;

        private Transform[] clouds;
        private float[] lanes;
        private float[] offsets;
        private float coverage;

        private void Start()
        {
            if (game == null) game = FindAnyObjectByType<GameManager>();
            clouds = new Transform[count];
            lanes = new float[count];
            offsets = new float[count];
            for (int i = 0; i < count; i++)
            {
                clouds[i] = Instantiate(cloudPrefab, transform).transform;
                lanes[i] = Mathf.Lerp(-halfWidth * 0.8f, halfWidth * 0.8f, (i + 0.5f) / count);
                offsets[i] = Random.Range(-halfWidth, halfWidth);
                clouds[i].localScale = Vector3.zero;
            }
        }

        private void Update()
        {
            if (game == null || clouds == null) return;
            bool running = game.Phase == GamePhase.Playing;
            float target = running ? game.Weather.Current.cloudCover : 0f;
            coverage = Mathf.MoveTowards(coverage, target, Time.deltaTime * 0.3f);
            float wind = running ? game.Weather.Current.windStrength : 0.2f;

            for (int i = 0; i < clouds.Length; i++)
            {
                if (running) offsets[i] += Time.deltaTime * driftSpeed * (0.5f + wind);
                if (offsets[i] > halfWidth) offsets[i] -= halfWidth * 2f;

                float visible = Mathf.Clamp01(coverage * clouds.Length - i);
                float edgeFade = 1f - Mathf.SmoothStep(0.75f, 1f, Mathf.Abs(offsets[i]) / halfWidth);
                float scale = cloudScale * visible * edgeFade * (0.8f + 0.2f * Mathf.Sin(i * 1.7f));
                clouds[i].localPosition = new Vector3(offsets[i], height + 0.01f * (i % 2), lanes[i]);
                clouds[i].localScale = Vector3.one * scale;
                clouds[i].gameObject.SetActive(scale > 0.02f);
            }
        }
    }
}
