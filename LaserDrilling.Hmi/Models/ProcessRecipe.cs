using System;
using System.Collections.Generic;
using System.Linq;

namespace LaserDrilling.Hmi.Models
{
    /// <summary>
    /// 表示功率曲线中一个进度与功率百分比节点。
    /// </summary>
    public sealed record PowerPoint(double Progress, double PowerPercent);
    /// <summary>
    /// 保存功率百分比、脉冲频率和脉冲数量等基础加工参数。
    /// </summary>
    public sealed record ProcessParameters(double PowerPercent, double FrequencyKhz, int PulseCount);
    /// <summary>
    /// 描述工艺文件及功率曲线，提供参数校验、快照和曲线插值。
    /// </summary>
    public sealed record ProcessRecipe(string Name, ProcessParameters Parameters)
    {
        public static IReadOnlyList<string> AvailableGases { get; } = Array.AsReadOnly(new[] { "N2", "O2", "AIR", "Ar" });
        public int SchemaVersion { get; init; } = 1;
        public string Material { get; init; } = "Demo alloy";
        public double ThicknessMm { get; init; } = 1;
        public string Nozzle { get; init; } = "DEMO-1";
        public string Gas { get; init; } = "N2";
        public double GasPressureBar { get; init; } = 1;
        public double FocusOffsetMm { get; init; }
        public double DwellMilliseconds { get; init; } = 900;
        public PowerPoint[] PowerCurve { get; init; } = new[] { new PowerPoint(0, 10), new PowerPoint(25, 35), new PowerPoint(75, 35), new PowerPoint(100, 15) };

        public static ProcessRecipe Template(string name = "DEFAULT") => new(name, new ProcessParameters(35, 50, 100));
        public ProcessRecipe Snapshot() => this with { PowerCurve = PowerCurve.ToArray() };
        public void Validate()
        {
            static bool Range(double x, double min, double max) => double.IsFinite(x) && x >= min && x <= max;
            if (SchemaVersion != 1 || string.IsNullOrWhiteSpace(Name) || Name.Length > 48 || string.IsNullOrWhiteSpace(Material) || Material.Length > 48 ||
                !Range(ThicknessMm, 0.01, 100) || string.IsNullOrWhiteSpace(Nozzle) || Nozzle.Length > 32 || !AvailableGases.Contains(Gas) ||
                !Range(GasPressureBar, 0, 30) || !Range(FocusOffsetMm, -100, 100) || !Range(DwellMilliseconds, 100, 60000) || Parameters == null ||
                !Range(Parameters.PowerPercent, 0, 100) || !Range(Parameters.FrequencyKhz, 0.001, 1000) || Parameters.PulseCount < 1 || Parameters.PulseCount > 1000000)
                throw new ArgumentException("Machine.InvalidRecipe");
            if (PowerCurve == null || PowerCurve.Length < 2 || PowerCurve.Length > 32 || PowerCurve.Any(p => p == null || !Range(p.Progress, 0, 100) || !Range(p.PowerPercent, 0, 100)) ||
                PowerCurve[0].Progress != 0 || PowerCurve[^1].Progress != 100 || PowerCurve.Zip(PowerCurve.Skip(1)).Any(p => p.First.Progress >= p.Second.Progress))
                throw new ArgumentException("Machine.InvalidCurve");
            if (PowerCurve.Any(p => p.PowerPercent > Parameters.PowerPercent))
                throw new ArgumentException("Machine.CurvePowerExceeded");
        }
        public double PowerAt(double progress)
        {
            progress = Math.Clamp(progress, 0, 100);
            for (int i = 1; i < PowerCurve.Length; i++)
                if (progress <= PowerCurve[i].Progress)
                {
                    var a = PowerCurve[i - 1]; var b = PowerCurve[i];
                    return a.PowerPercent + (b.PowerPercent - a.PowerPercent) * (progress - a.Progress) / (b.Progress - a.Progress);
                }
            return PowerCurve[^1].PowerPercent;
        }
    }
}
