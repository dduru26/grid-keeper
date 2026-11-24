namespace GridKeeper.Energy
{
    public interface IStorage
    {
        float StoredMWh { get; }
        float CapacityMWh { get; }
        void BeginTick();
        float Charge(float surplusMW, float hours);
        float Discharge(float shortfallMW, float hours);
    }
}
