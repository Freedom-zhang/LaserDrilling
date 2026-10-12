using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LaserDrilling.Hmi.ViewModels;

namespace LaserDrilling.Hmi.Views
{
    // Rendering and hit-testing only; processing state remains in the mock service.
    public sealed class DrillingViewport : FrameworkElement
    {
        public static readonly DependencyProperty RevisionProperty = DependencyProperty.Register(nameof(Revision), typeof(int), typeof(DrillingViewport), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
        public int Revision { get => (int)GetValue(RevisionProperty); set => SetValue(RevisionProperty, value); }
        private double _zoom = 1;
        private double Scale => Math.Min(Math.Max(1, ActualWidth - 100) / 100, Math.Max(1, ActualHeight - 100) / 80) * _zoom;
        private Point P(double x, double y) => new Point(ActualWidth / 2 + (x - 50) * Scale, ActualHeight / 2 - (y - 40) * Scale);
        private static Brush B(string color) => (Brush)new BrushConverter().ConvertFromString(color);
        public void Fit() { _zoom = 1; InvalidateVisual(); }
        private void Text(DrawingContext dc, string text, Point p, string color = "#7B98AB", double size = 11)
        {
            dc.DrawText(new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas"), size, B(color), VisualTreeHelper.GetDpi(this).PixelsPerDip), p);
        }
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            dc.DrawRectangle(B("#142633"), null, new Rect(RenderSize));
            dc.PushClip(new RectangleGeometry(new Rect(RenderSize)));
            for (var x = 0; x <= 100; x += 10) { dc.DrawLine(new Pen(B("#29404F"), 1), P(x, 0), P(x, 80)); Text(dc, x.ToString(), new Point(P(x, 0).X - 6, P(x, 0).Y + 9)); }
            for (var y = 0; y <= 80; y += 10) { dc.DrawLine(new Pen(B("#29404F"), 1), P(0, y), P(100, y)); Text(dc, y.ToString(), new Point(P(0, y).X - 26, P(0, y).Y - 6)); }
            dc.DrawRectangle(null, new Pen(B("#7595A7"), 1), new Rect(P(0, 80), P(100, 0)));
            Text(dc, "100.000 mm", new Point(ActualWidth/2 - 30, P(0, 80).Y - 24));
            if (DataContext is DrillingViewModel vm)
            {
                if (vm.Machine.IsFraming)
                {
                    var framePen = new Pen(B("#FF725D"), 2) { DashStyle = DashStyles.Dash };
                    for (int i = 1; i < vm.Machine.FramePath.Count; i++)
                    {
                        var previous = vm.Machine.FramePath[i-1]; var next = vm.Machine.FramePath[i];
                        dc.DrawLine(framePen, P(previous.X, previous.Y), P(next.X, next.Y));
                    }
                }
                var travel = new Pen(B("#4D7589"), 1) { DashStyle = DashStyles.Dash };
                for (int i = 1; i < vm.Machine.Holes.Count; i++) dc.DrawLine(travel, P(vm.Machine.Holes[i-1].X, vm.Machine.Holes[i-1].Y), P(vm.Machine.Holes[i].X, vm.Machine.Holes[i].Y));
                foreach (var h in vm.Machine.Holes)
                {
                    var point = P(h.X, h.Y); var radius = Math.Max(4, h.Diameter * Scale / 2);
                    dc.DrawEllipse(vm.Machine.IsDryRun && h.Visited ? B("#64B5F6") : h.Done ? B("#48BCB4") : B("#142633"), new Pen(B(h.Done ? "#48BCB4" : "#AAC7D5"), 1.5), point, radius, radius);
                    if (h == vm.Selected) dc.DrawEllipse(null, new Pen(B("#F2BD69"), 1.5), point, radius+5, radius+5);
                    Text(dc, h.Number.ToString("00"), new Point(point.X+7, point.Y-17), "#8DAEBE", 10);
                }
                Text(dc, $"Z {vm.Machine.Z:F3} mm", new Point(16,34), "#D0E3ED");
                var beam = P(vm.Machine.X, vm.Machine.Y); var pen = new Pen(B(vm.Machine.LaserOn ? "#FFB64D" : vm.Machine.RedLightOn ? "#FF5050" : "#59C5D0"), 1.5);
                dc.DrawLine(pen, new Point(beam.X-13, beam.Y), new Point(beam.X+13, beam.Y)); dc.DrawLine(pen, new Point(beam.X, beam.Y-13), new Point(beam.X, beam.Y+13));
                if (vm.Machine.LaserOn) dc.DrawEllipse(B("#FFE2A1"), null, beam, 3, 3);
            }
            var origin = P(0, 0); dc.DrawLine(new Pen(B("#E87573"),2), origin, new Point(origin.X+24,origin.Y)); dc.DrawLine(new Pen(B("#60BBA7"),2),origin,new Point(origin.X,origin.Y-24));
            Text(dc,"X",new Point(origin.X+26,origin.Y-7),"#E87573"); Text(dc,"Y",new Point(origin.X-4,origin.Y-39),"#60BBA7");
            Text(dc,"G54  /  XY", new Point(16,14)); Text(dc, $"{_zoom*100:0}%",new Point(ActualWidth-50,14));
            dc.Pop();
        }
        protected override void OnMouseWheel(MouseWheelEventArgs e) { _zoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.1 : 1/1.1), 0.5, 3); InvalidateVisual(); e.Handled = true; }
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (DataContext is DrillingViewModel vm)
            {
                var point = e.GetPosition(this);
                foreach (var h in vm.Machine.Holes) if ((P(h.X,h.Y)-point).Length < 12) { vm.Selected = h; InvalidateVisual(); break; }
            }
            base.OnMouseDown(e);
        }
    }
}
