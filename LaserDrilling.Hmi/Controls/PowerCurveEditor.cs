using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LaserDrilling.Hmi.ViewModels;

namespace LaserDrilling.Hmi.Controls
{
    /// <summary>
    /// 绘制功率曲线并处理节点拖动，曲线数据及插值规则由工艺模型维护。
    /// </summary>
    public sealed class PowerCurveEditor : FrameworkElement
    {
        public static readonly DependencyProperty RevisionProperty = DependencyProperty.Register(nameof(Revision), typeof(int), typeof(PowerCurveEditor), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
        public int Revision { get => (int)GetValue(RevisionProperty); set => SetValue(RevisionProperty, value); }
        private int _drag = -1;
        private Rect Plot => new(44, 24, Math.Max(1, ActualWidth - 64), Math.Max(1, ActualHeight - 62));
        private Point P(double x, double y) => new(Plot.Left + Plot.Width * x / 100, Plot.Bottom - Plot.Height * y / 100);
        private static Brush B(string c) => (Brush)new BrushConverter().ConvertFromString(c);
        private void Text(DrawingContext dc, string text, Point p) => dc.DrawText(new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas"), 10, B("#A8BFCA"), VisualTreeHelper.GetDpi(this).PixelsPerDip), p);
        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(B("#142633"), null, new Rect(RenderSize));
            for (int i = 0; i <= 100; i += 20)
            {
                dc.DrawLine(new Pen(B("#304957"), 1), P(i, 0), P(i, 100));
                dc.DrawLine(new Pen(B("#304957"), 1), P(0, i), P(100, i));
                Text(dc, i.ToString(), new Point(P(i, 0).X - 6, Plot.Bottom + 7)); Text(dc, i.ToString(), new Point(12, P(0, i).Y - 6));
            }
            Text(dc, "POWER / %", new Point(10, 5)); Text(dc, "PROGRESS / %", new Point(Plot.Right - 80, ActualHeight - 14));
            if (DataContext is not RecipeLibraryViewModel vm) return;
            for (int i = 1; i < vm.Points.Count; i++) dc.DrawLine(new Pen(B("#52CBD1"), 2), P(vm.Points[i-1].Progress, vm.Points[i-1].PowerPercent), P(vm.Points[i].Progress, vm.Points[i].PowerPercent));
            foreach (var point in vm.Points) dc.DrawEllipse(B(point == vm.SelectedPoint ? "#F2BE6A" : "#52CBD1"), new Pen(B("#142633"), 2), P(point.Progress, point.PowerPercent), 6, 6);
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (DataContext is not RecipeLibraryViewModel vm || !vm.CanEdit) return;
            var mouse = e.GetPosition(this);
            for(int i=0;i<vm.Points.Count;i++)
                if ((P(vm.Points[i].Progress,vm.Points[i].PowerPercent)-mouse).Length<12) { _drag=i; vm.SelectedPoint=vm.Points[i]; CaptureMouse(); InvalidateVisual(); e.Handled=true; break; }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_drag<0 || !IsMouseCaptured || e.LeftButton!=MouseButtonState.Pressed || DataContext is not RecipeLibraryViewModel vm) return;
            var point=e.GetPosition(this); vm.MovePoint(_drag,(point.X-Plot.Left)/Plot.Width*100,(Plot.Bottom-point.Y)/Plot.Height*100);
        }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) { _drag=-1; ReleaseMouseCapture(); }
        protected override void OnLostMouseCapture(MouseEventArgs e) { _drag=-1; base.OnLostMouseCapture(e); }
    }
}
