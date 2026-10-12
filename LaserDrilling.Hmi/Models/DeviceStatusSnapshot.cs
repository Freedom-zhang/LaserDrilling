namespace LaserDrilling.Hmi.Models
{
    // Demonstration equipment profile and telemetry. Values are not real hardware specifications.
    public sealed record DeviceStatusSnapshot
    {
        public string ControllerModel { get; init; } = "MOCK-MC3";
        public int RigidityLevel { get; init; } = 17;
        public string FirmwareVersion { get; init; } = "1.0.0-demo";
        public double X { get; init; }
        public double Y { get; init; }
        public double Z { get; init; }
        public double TrackingErrorMm { get; init; }
        public int IoModules { get; init; } = 2;
        public int InputPorts { get; init; } = 32;
        public int OutputPorts { get; init; } = 16;
        public int ActiveInputs { get; init; }
        public int ActiveOutputs { get; init; }
        public string HeightControllerVersion { get; init; } = "MOCK-HC / 1.0";
        public int FollowLevel { get; init; } = 18;
        public double CalibrationRangeMm { get; init; } = 10;
        public double CapacitancePf { get; init; } = 12.5;
        public double HeightDeviationMm { get; init; }
        public string LastCalibration { get; init; } = "2026-10-12 / MOCK";
        public string HeadModel { get; init; } = "MOCK-DRILL-01";
        public string FocusRange { get; init; } = "-50 ~ +50 mm";
        public double FocusMm { get; init; }
        public double SpotDiameterMm { get; init; } = 0.05;
        public string LaserModel { get; init; } = "MOCK-PULSE-100";
        public double RatedPowerW { get; init; } = 100;
        public double PowerPercent { get; init; }
        public double SimulatedPowerW { get; init; }
        public double FrequencyKhz { get; init; }
        public string ValveRange { get; init; } = "0 ~ 10 V";
        public double GasPressureLimitBar { get; init; } = 30;
        public string GasType { get; init; } = "N2";
        public double GasPressureBar { get; init; }
        public string CameraModel { get; init; } = "MOCK-CAM-5M";
        public int CameraCount { get; init; } = 1;
        public string CameraResolution { get; init; } = "2448 × 2048 px";
        public double ExposureMs { get; init; } = 5;
        public string PeripheralModel { get; init; } = "MOCK-CHILLER / EXTRACTOR";
        public double WaterTemperatureC { get; init; } = 23.5;
        public double WaterFlowLpm { get; init; } = 4.2;
        public double SupplyVoltageV { get; init; } = 24;
    }
}
