using GridKeeper.World;
using UnityEngine;

namespace GridKeeper.Presentation
{
    [RequireComponent(typeof(Building))]
    public class FactorySmoke : MonoBehaviour
    {
        [SerializeField] private Transform chimneyTop;
        [SerializeField] private GameObject puffPrefab;
        [SerializeField, Min(1)] private int poolSize = 6;
        [SerializeField] private float interval = 0.45f;
        [SerializeField] private float lifetime = 2.4f;
        [SerializeField] private float rise = 0.05f;

        private Building building;
        private Core.GameManager game;
        private Transform[] puffs;
        private float[] ages;
        private float timer;
        private int next;

        private void Start()
        {
            building = GetComponent<Building>();
            game = FindAnyObjectByType<Core.GameManager>();
            puffs = new Transform[poolSize];
            ages = new float[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                puffs[i] = Instantiate(puffPrefab, chimneyTop).transform;
                puffs[i].gameObject.SetActive(false);
                ages[i] = lifetime;
            }
        }

        private void Update()
        {
            if (puffs == null) return;
            bool working = building.IsLit && building.DemandAt(CurrentHour()) > building.Definition.PeakMW * 0.5f;

            timer -= Time.deltaTime;
            if (working && timer <= 0f)
            {
                timer = interval;
                ages[next] = 0f;
                puffs[next].gameObject.SetActive(true);
                next = (next + 1) % poolSize;
            }

            for (int i = 0; i < poolSize; i++)
            {
                if (ages[i] >= lifetime) continue;
                ages[i] += Time.deltaTime;
                float k = ages[i] / lifetime;
                puffs[i].localPosition = new Vector3(Mathf.Sin(k * 3f + i) * 0.004f, k * rise, 0f);
                puffs[i].localScale = Vector3.one * Mathf.Lerp(0.004f, 0.014f, k) * (1f - Mathf.SmoothStep(0.7f, 1f, k));
                if (ages[i] >= lifetime) puffs[i].gameObject.SetActive(false);
            }
        }

        private float CurrentHour()
        {
            return game != null && game.Clock != null ? game.Clock.SmoothHourOfDay : 12f;
        }
    }
}
