namespace LaserDrilling.Hmi.Models
{
    // Demonstration timing and travel limits; independent of presentation and real hardware.
    public sealed record SimulationParameters
    {
        public int TickMilliseconds { get; init; } = 100;
        public int HomingTicks { get; init; } = 20;
        public int TravelTicks { get; init; } = 6;
        public double JogSpeedMmPerSecond { get; init; } = 5;
        public double TravelXmm { get; init; } = 100;
        public double TravelYmm { get; init; } = 80;
        public double ChillerCelsius { get; init; } = 23.5;
        public int SampleRows { get; init; } = 4;
        public int SampleColumns { get; init; } = 6;
        public double SampleOriginX { get; init; } = 15;
        public double SampleOriginY { get; init; } = 15;
        public double SamplePitchX { get; init; } = 14;
        public double SamplePitchY { get; init; } = 16;
    }
}
