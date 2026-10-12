using System;
using System.Collections.Generic;

namespace LaserDrilling.Hmi.Models
{
    /// <summary>
    /// 记录一次保养的时间、项目、操作人、备注及累计使用时长。
    /// </summary>
    public sealed record MaintenanceEntry(DateTimeOffset Time, string Item, string User, string Note, double MeterHours);
    /// <summary>
    /// 保存设备累计使用时间、保养基准及历史记录，供本地持久化。
    /// </summary>
    public sealed class MaintenanceData
    {
        public double PowerSeconds { get; set; }
        public double EmissionSeconds { get; set; }
        public double MotionSeconds { get; set; }
        public double GasSeconds { get; set; }
        public Dictionary<string, double> LastServiceHours { get; set; } = new();
        public List<MaintenanceEntry> Records { get; set; } = new();
    }
    /// <summary>
    /// 定义保养项目的周期及使用的累计计时指标。
    /// </summary>
    public sealed record MaintenanceSchedule(string Key, double IntervalHours, string Meter)
    {
        public static MaintenanceSchedule[] Defaults { get; } = {
            new("LaserService", 500, "Power"), new("LensService", 100, "Emission"),
            new("MotionService", 250, "Motion"), new("CoolingService", 500, "Power")
        };
    }
}
