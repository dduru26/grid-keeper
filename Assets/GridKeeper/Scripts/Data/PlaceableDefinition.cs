using UnityEngine;

namespace GridKeeper.Data
{
    [CreateAssetMenu(menuName = "Grid Keeper/Placeable Definition", fileName = "Placeable")]
    public class PlaceableDefinition : ScriptableObject
    {
        [SerializeField] private SourceKind kind;
        [SerializeField] private string displayName = "Source";
        [SerializeField, Min(0)] private int cost = 100;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Color accent = Color.white;

        [Header("Generation")]
        [SerializeField, Min(0f), Tooltip("Output in MW at full sun or full wind.")]
        private float ratedMW;

        [Header("Storage")]
        [SerializeField, Min(0f)] private float capacityMWh;
        [SerializeField, Min(0f)] private float maxRateMW;

        public SourceKind Kind => kind;
        public string DisplayName => displayName;
        public int Cost => cost;
        public GameObject Prefab => prefab;
        public Color Accent => accent;
        public float RatedMW => ratedMW;
        public float CapacityMWh => capacityMWh;
        public float MaxRateMW => maxRateMW;

        public void Configure(SourceKind newKind, string newName, int newCost, GameObject newPrefab, Color newAccent, float rated, float capacity, float rate)
        {
            kind = newKind;
            displayName = newName;
            cost = newCost;
            prefab = newPrefab;
            accent = newAccent;
            ratedMW = rated;
            capacityMWh = capacity;
            maxRateMW = rate;
        }
    }
}
