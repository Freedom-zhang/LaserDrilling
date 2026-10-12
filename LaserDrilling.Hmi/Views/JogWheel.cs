using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LaserDrilling.Hmi.ViewModels;

namespace LaserDrilling.Hmi.Views
{
    public sealed class JogWheel : FrameworkElement
    {
        private Window _owner;
        private int _sector = -1;
        public JogWheel()
        {
            Focusable = true;
            Loaded += (_, _) => { _owner = Window.GetWindow(this); if (_owner != null) _owner.Deactivated += OnDeactivate; };
            Unloaded += (_, _) => { Release(); if (_owner != null) _owner.Deactivated -= OnDeactivate; _owner = null; };
            IsEnabledChanged += (_, _) => { if (!IsEnabled) Release(); InvalidateVisual(); };
        }
        private void OnDeactivate(object sender, EventArgs e) => Release();
        private Brush Color(string hex) => (Brush)new BrushConverter().ConvertFromString(hex);
        private void Text(DrawingContext dc, string value, double x, double y, string color, double size = 15)
        {
            var text = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, Color(color), VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(text, new Point(x-text.Width/2, y-text.Height/2));
        }
        protected override void OnRender(DrawingContext dc)
        {
            var c = new Point(ActualWidth/2, ActualHeight/2); var r = Math.Min(ActualWidth,ActualHeight)/2 - 5;
            dc.DrawEllipse(Color("#213746"), new Pen(Color("#56707F"),1), c,r,r);
            for (int i = 0; i < 8; i++)
            {
                var a = i*Math.PI/4; var p = new Point(c.X+Math.Cos(a)*r*.70,c.Y-Math.Sin(a)*r*.70);
                if (_sector == i) dc.DrawEllipse(Color("#278E9B"), null,p,21,21);
                var arrows = new[] { "→", "↗", "↑", "↖", "←", "↙", "↓", "↘" };
                Text(dc, arrows[i],p.X,p.Y,IsEnabled ? "#DCE9F0" : "#728592",22);
            }
            dc.DrawEllipse(Color("#152631"),new Pen(Color("#506C7E"),1),c,r*.28,r*.28);
            Text(dc,"XY",c.X,c.Y,IsEnabled ? "#72D2DA" : "#728592",14);
        }
        private void Drive(Point point)
        {
            var x=point.X-ActualWidth/2; var y=ActualHeight/2-point.Y; var length=Math.Sqrt(x*x+y*y);
            if (length < 25 || length > Math.Min(ActualWidth,ActualHeight)/2) { Release(); return; }
            _sector = ((int)Math.Round(Math.Atan2(y,x)/(Math.PI/4))+8)%8;
            if (DataContext is DrillingViewModel vm) vm.Machine.BeginJog(Math.Cos(_sector*Math.PI/4), Math.Sin(_sector*Math.PI/4));
            InvalidateVisual();
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) { if (!IsEnabled) return; Focus(); CaptureMouse(); Drive(e.GetPosition(this)); e.Handled=true; }
        protected override void OnMouseMove(MouseEventArgs e) { if (IsMouseCaptured && e.LeftButton==MouseButtonState.Pressed) Drive(e.GetPosition(this)); }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) { Release(); e.Handled=true; }
        protected override void OnLostMouseCapture(MouseEventArgs e) { Release(); base.OnLostMouseCapture(e); }
        protected override void OnMouseLeave(MouseEventArgs e) { Release(); base.OnMouseLeave(e); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.Key==Key.Escape) Release(); base.OnKeyDown(e); }
        public void Release()
        {
            _sector=-1;
            if (DataContext is DrillingViewModel vm && vm.Machine.IsJogging) vm.Machine.EndJog();
            if (IsMouseCaptured) ReleaseMouseCapture();
            InvalidateVisual();
        }
    }
}
