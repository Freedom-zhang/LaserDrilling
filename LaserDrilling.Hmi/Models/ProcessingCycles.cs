namespace LaserDrilling.Hmi.Models
{
    /// <summary>
    /// 保存本次运行的目标次数和完成次数；一轮覆盖全部孔位，空走单独标记。
    /// </summary>
    public sealed record ProcessingCycles
    {
        public int Target { get; init; } = 1;
        public int Completed { get; init; }
        public bool IsPreview { get; init; }
        public string Display => $"{Completed:00} / {Target:00}";
    }
}
