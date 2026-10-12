using Prism.Mvvm;
namespace LaserDrilling.Hmi.Models
{
    /// <summary>
    /// 定义设备模拟运行的状态，包括待机、回零、加工、暂停及故障。
    /// </summary>
    public enum MachineState { Idle, Homing, Ready, Running, Paused, Completed, Stopped, Fault }
    /// <summary>
    /// 保存单个孔位的几何参数及加工、空走状态，并通知界面更新。
    /// </summary>
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
    /// <summary>
    /// 保存一条设备事件的时间、级别及描述。
    /// </summary>
    public sealed record MachineEvent(string Time, string Level, string Message);
}
