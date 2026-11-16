namespace GridKeeper.Energy
{
    public abstract class PowerSource : Placeable
    {
        public float LastOutputMW { get; private set; }

        public float Generate(float hour, WeatherSample weather)
        {
            LastOutputMW = OutputMW(hour, weather);
            return LastOutputMW;
        }

        protected abstract float OutputMW(float hour, WeatherSample weather);
    }
}
