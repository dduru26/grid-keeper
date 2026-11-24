namespace GridKeeper.Energy
{
    public enum GeneratorKind
    {
        Solar,
        Wind
    }

    public interface IGenerator
    {
        GeneratorKind Kind { get; }
        float Generate(float hour, WeatherSample weather);
    }
}
