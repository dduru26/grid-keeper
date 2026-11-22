using GridKeeper.AR;
using UnityEngine;

namespace GridKeeper.Core
{
    public class PerformanceBootstrap : MonoBehaviour
    {
        [SerializeField, Tooltip("The FPS benchmark the game is profiled against on iPhone XR.")]
        private int targetFrameRate = 60;
        [SerializeField] private ARPlacementController placement;
        [SerializeField, Tooltip("Static geometry combined into batches once the town is anchored.")]
        private Transform staticRoot;

        public int TargetFrameRate => targetFrameRate;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (placement == null) placement = FindAnyObjectByType<ARPlacementController>();
        }

        private void OnEnable()
        {
            if (placement != null) placement.CityPlaced += CombineStatic;
        }

        private void OnDisable()
        {
            if (placement != null) placement.CityPlaced -= CombineStatic;
        }

        private void CombineStatic(Transform city)
        {
            if (staticRoot == null) return;
            StaticBatchingUtility.Combine(staticRoot.gameObject);
        }
    }
}
