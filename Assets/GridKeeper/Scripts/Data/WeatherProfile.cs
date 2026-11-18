using UnityEngine;

namespace GridKeeper.Data
{
    public enum WeatherKind
    {
        Clear,
        Cloudy,
        Windy,
        Heatwave
    }

    [CreateAssetMenu(menuName = "Grid Keeper/Weather Profile", fileName = "Weather")]
    public class WeatherProfile : ScriptableObject
    {
        [SerializeField] private WeatherKind kind;
        [SerializeField] private string label = "Clear skies";
        [SerializeField, TextArea] private string advice = "Plenty of sun. Store some for the evening.";
        [SerializeField] private Vector2 cloudRange = new Vector2(0f, 0.15f);
        [SerializeField] private Vector2 windRange = new Vector2(0.25f, 0.5f);
        [SerializeField] private bool heatwave;
        [SerializeField] private Color tint = Color.white;

        public WeatherKind Kind => kind;
        public string Label => label;
        public string Advice => advice;
        public Vector2 CloudRange => cloudRange;
        public Vector2 WindRange => windRange;
        public bool Heatwave => heatwave;
        public Color Tint => tint;

        public void Configure(WeatherKind newKind, string newLabel, string newAdvice, Vector2 cloud, Vector2 wind, bool isHeatwave, Color newTint)
        {
            kind = newKind;
            label = newLabel;
            advice = newAdvice;
            cloudRange = cloud;
            windRange = wind;
            heatwave = isHeatwave;
            tint = newTint;
        }
    }
}
