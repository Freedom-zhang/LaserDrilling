using Prism.Mvvm;
namespace LaserDrilling.Hmi.Models
{
    public enum MachineState { Idle, Homing, Ready, Running, Paused, Completed, Stopped, Fault }
    public sealed class DrillHole : BindableBase
    {
        public int Number { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Diameter { get; set; } = 0.5;
        public double Depth { get; set; } = 0.8;
        private bool _done;
        public bool Done { get => _done; set => SetProperty(ref _done, value); }
        private bool _visited;
        public bool Visited { get => _visited; set => SetProperty(ref _visited, value); }
    }
    public sealed record MachineEvent(string Time, string Level, string Message);
}
