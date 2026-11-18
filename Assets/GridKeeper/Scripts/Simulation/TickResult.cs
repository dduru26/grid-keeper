namespace GridKeeper.Simulation
{
    public struct TickResult
    {
        public float ElapsedHours;
        public float HourOfDay;
        public float DemandMW;
        public float SolarMW;
        public float WindMW;
        public float ChargedMW;
        public float DischargedMW;
        public float DeliveredMW;
        public float UnmetMW;
        public float WastedMW;
        public float StoredMWh;
        public float CapacityMWh;

        public float GenerationMW => SolarMW + WindMW;
        public bool IsBlackout => UnmetMW > 0.01f;
        public float Charge01 => CapacityMWh <= 0f ? 0f : StoredMWh / CapacityMWh;
    }
}
