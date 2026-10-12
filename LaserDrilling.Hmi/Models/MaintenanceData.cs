using System;
using System.Collections.Generic;

namespace LaserDrilling.Hmi.Models
{
    public sealed record MaintenanceEntry(DateTimeOffset Time, string Item, string User, string Note, double MeterHours);
    public sealed class MaintenanceData
    {
        public double PowerSeconds { get; set; }
        public double EmissionSeconds { get; set; }
        public double MotionSeconds { get; set; }
        public double GasSeconds { get; set; }
        public Dictionary<string, double> LastServiceHours { get; set; } = new();
        public List<MaintenanceEntry> Records { get; set; } = new();
    }
    public sealed record MaintenanceSchedule(string Key, double IntervalHours, string Meter)
    {
        public static MaintenanceSchedule[] Defaults { get; } = {
            new("LaserService", 500, "Power"), new("LensService", 100, "Emission"),
            new("MotionService", 250, "Motion"), new("CoolingService", 500, "Power")
        };
    }
}
