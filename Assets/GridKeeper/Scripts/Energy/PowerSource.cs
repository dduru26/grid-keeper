namespace GridKeeper.Energy
{
    public abstract class PowerSource : Placeable, IGenerator
    {
        public float LastOutputMW { get; private set; }
        public abstract GeneratorKind Kind { get; }

        public float Generate(float hour, WeatherSample weather)
        {
            LastOutputMW = OutputMW(hour, weather);
            return LastOutputMW;
        }

        protected abstract float OutputMW(float hour, WeatherSample weather);
    }
}
