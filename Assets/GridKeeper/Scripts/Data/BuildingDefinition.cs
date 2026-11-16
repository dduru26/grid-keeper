using UnityEngine;

namespace GridKeeper.Data
{
    [CreateAssetMenu(menuName = "Grid Keeper/Building Definition", fileName = "Building")]
    public class BuildingDefinition : ScriptableObject
    {
        [SerializeField] private BuildingKind kind;
        [SerializeField] private string displayName = "Building";
        [SerializeField, Min(0f)] private float peakMW = 1f;
        [SerializeField, Tooltip("Share of peak demand across the day. X is the hour from 0 to 24, Y is 0 to 1.")]
        private AnimationCurve dailyCurve = AnimationCurve.Constant(0f, 24f, 1f);

        public BuildingKind Kind => kind;
        public string DisplayName => displayName;
        public float PeakMW => peakMW;

        public float DemandAt(float hour)
        {
            return peakMW * Mathf.Clamp01(dailyCurve.Evaluate(Mathf.Repeat(hour, 24f)));
        }

        public void Configure(BuildingKind newKind, string newName, float peak, AnimationCurve curve)
        {
            kind = newKind;
            displayName = newName;
            peakMW = peak;
            dailyCurve = curve;
        }
    }
}
