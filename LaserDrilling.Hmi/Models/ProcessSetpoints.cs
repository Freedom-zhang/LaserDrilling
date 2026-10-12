using Prism.Mvvm;
namespace LaserDrilling.Hmi.Models
{
    /// <summary>
    /// 保存加工设定的编辑草稿，供界面绑定并在应用时转换为加工参数。
    /// </summary>
    public sealed class ProcessSetpoints : BindableBase
    {
        private double _power, _frequency;
        private int _pulses;
        private int _cycles = 1;
        public int Cycles { get => _cycles; set => SetProperty(ref _cycles, value); }
        public double Power { get => _power; set => SetProperty(ref _power, value); }
        public double Frequency { get => _frequency; set => SetProperty(ref _frequency, value); }
        public int Pulses { get => _pulses; set => SetProperty(ref _pulses, value); }
        public ProcessParameters ToParameters() => new(Power, Frequency, Pulses);
    }
}
