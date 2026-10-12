namespace LaserDrilling.Hmi.Models
{
    // One cycle processes the complete hole list. Dry-run counters are preview-only.
    public sealed record ProcessingCycles
    {
        public int Target { get; init; } = 1;
        public int Completed { get; init; }
        public bool IsPreview { get; init; }
        public string Display => $"{Completed:00} / {Target:00}";
    }
}
