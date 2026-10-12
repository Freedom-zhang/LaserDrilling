using System;
using System.Linq;
using System.Collections.ObjectModel;
using Prism.Mvvm;

namespace LaserDrilling.Hmi.Services
{
    // Deterministic mock only. No laser, motion-card or PLC communication.
    public sealed class MockDrillingMachine : BindableBase, IDisposable
    {
        private readonly IAccountService _accounts;
        private readonly System.Windows.Threading.DispatcherTimer _timer;
        private int _tick, _index, _revision;
        private double _fromX, _fromY;
        private double _jogX, _jogY, _jogZ;
        private int _testTicks, _frameIndex;
        private (double X, double Y)[] _frame = Array.Empty<(double, double)>();
        public bool ShutterOpen { get; private set; }
        public bool RedLightOn { get; private set; }
        public bool FollowEnabled { get; private set; }
        public bool IsTesting => _testTicks > 0;
        public bool IsFraming { get; private set; }
        public System.Collections.Generic.IReadOnlyList<(double X, double Y)> FramePath => _frame;
        private double _jogOverride = 100;
        public double JogOverride { get => _jogOverride; set { if (double.IsFinite(value)) { _jogOverride = Math.Clamp(value, 10, 100); Changed(); } } }
        public bool CanAuxiliary => CanHome && _accounts.CurrentUser != null;
        public bool CanTest => CanAuxiliary && Homed && ShutterOpen && !RedLightOn;
        public bool CanZJog => CanJog && !FollowEnabled;
        public bool HasOutputs => ShutterOpen || RedLightOn || AirOn || LaserOn;
        public void ToggleShutter() { Authenticate(); if (!CanAuxiliary) return; ShutterOpen = !ShutterOpen; Changed(); }
        public void ToggleRedLight() { Authenticate(); if (!CanAuxiliary) return; RedLightOn = !RedLightOn; Changed(); }
        public void ToggleAir() { Authenticate(); if (!CanAuxiliary) return; AirOn = !AirOn; Changed(); }
        public void ToggleFollow() { Authenticate(); if (!CanAuxiliary || !Homed) return; FollowEnabled = !FollowEnabled; if (FollowEnabled) Z = Math.Clamp(_activeRecipe.FocusOffsetMm, Simulation.ZMinimum, Simulation.ZMaximum); Changed(); }
        private void OutputsOff() { ShutterOpen = RedLightOn = LaserOn = AirOn = false; _testTicks = 0; IsFraming = false; }
        public void EndManualTest() { if (!IsTesting) return; OutputsOff(); Changed(); }
        public void TestEmission() { Authenticate(); if (!CanTest) return; _testTicks = Simulation.TestEmissionTicks; LaserOn = true; AirOn = false; Log("WARN", "Machine.TestEmission"); Changed(); }
        public void BeginZJog(double direction) { Authenticate(); if (!CanZJog || !double.IsFinite(direction) || direction == 0) return; OutputsOff(); _jogX = _jogY = 0; _jogZ = Math.Sign(direction); IsJogging = true; Changed(); }
        public void Frame()
        {
            Authenticate(); if (!CanStart) return;
            OutputsOff(); var left = Math.Max(0, Holes.Min(h => h.X-h.Diameter/2)-2); var right = Math.Min(Simulation.TravelXmm, Holes.Max(h => h.X+h.Diameter/2)+2);
            var bottom = Math.Max(0, Holes.Min(h => h.Y-h.Diameter/2)-2); var top = Math.Min(Simulation.TravelYmm, Holes.Max(h => h.Y+h.Diameter/2)+2);
            _frame = new[] { (left,bottom), (right,bottom), (right,top), (left,top), (left,bottom) }; _frameIndex = 0; IsFraming = true; RedLightOn = true; Log("INFO", "Machine.Frame"); Changed();
        }
        public SimulationParameters Simulation { get; } = new SimulationParameters();
        private ProcessRecipe _activeRecipe;
        public string ActiveRecipeName => _activeRecipe?.Name ?? "—";
        public double HoleProgress => Math.Clamp((_tick - Simulation.TravelTicks - 1) * Simulation.TickMilliseconds / (_activeRecipe?.DwellMilliseconds ?? 900) * 100, 0, 100);
        public double CurrentPowerPercent => LaserOn ? IsTesting ? Simulation.TestPowerPercent : _activeRecipe.PowerAt(HoleProgress) : 0;
        public string ConnectionText => App.Appearance.Text("Machine.MockConnected");
        public string MotionText => IsFraming ? App.Appearance.Text("Machine.Frame") : State == MachineState.Fault ? StateText : IsJogging ? App.Appearance.Text("Machine.ManualMoving") : Busy ? StateText : ReferenceText;
        public bool IsJogging { get; private set; }
        public bool IsDryRun { get; private set; }
        public bool AirOn { get; private set; }
        public bool CanJog => !Busy && Homed && DoorClosed && !EmergencyLatched && State != MachineState.Fault && _accounts.CurrentUser != null;
        public string ModeText => App.Appearance.Text(IsTesting ? "Machine.TestEmission" : IsFraming ? "Machine.Frame" : IsDryRun ? "Machine.DryMode" : "Machine.CutMode");
        public string CountLabel => App.Appearance.Text(IsDryRun ? "Machine.VisitedCount" : "Machine.CompletedLabel");
        public string AirText => App.Appearance.Text(AirOn ? "Machine.AirOn" : "Machine.AirOff");
        public ObservableCollection<DrillHole> Holes { get; } = new ObservableCollection<DrillHole>();
        public ObservableCollection<MachineEvent> Events { get; } = new ObservableCollection<MachineEvent>();
        public MachineState State { get; private set; } = MachineState.Idle;
        public bool Homed { get; private set; }
        public bool DoorClosed { get; private set; } = true;
        public bool EmergencyLatched { get; private set; }
        public bool LaserOn { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }
        public int Completed { get; private set; }
        public int Revision => _revision;
        public bool Busy => IsTesting || IsFraming || State == MachineState.Running || State == MachineState.Paused || State == MachineState.Homing;
        public bool CanHome => !Busy && !IsJogging && DoorClosed && !EmergencyLatched && State != MachineState.Fault;
        public bool CanStart => CanHome && Homed && Holes.Count > 0;
        public bool CanEdit => !Busy && !IsJogging && State != MachineState.Fault && _accounts.CurrentUser?.Role == UserRole.Administrator;
        public double Progress => Holes.Count == 0 ? 0 : 100.0 * Completed / Holes.Count;
        public double Elapsed { get; private set; }
        public string StateText => App.Appearance.Text("Machine." + (IsDryRun && State == MachineState.Running ? "DryRunning" : IsDryRun && State == MachineState.Completed ? "DryComplete" : State.ToString()));
        public string LaserText => App.Appearance.Text(State == MachineState.Fault ? "Machine.EmissionInhibited" : LaserOn ? "Machine.Emitting" : "Machine.Standby");
        public string DoorText => App.Appearance.Text(DoorClosed ? "Machine.Closed" : "Machine.Open");
        public string ReferenceText => App.Appearance.Text(Homed ? "Machine.Referenced" : "Machine.NotReferenced");
        public string ProgressText => $"{Completed:00} / {Holes.Count:00}";
        public string ActiveHole => State == MachineState.Running || State == MachineState.Paused ? $"H{_index + 1:00}" : "—";
        public string ElapsedText => TimeSpan.FromSeconds(Elapsed).ToString(@"mm\:ss");
        public string RecipeText { get; private set; }
        public MockDrillingMachine(IAccountService accounts)
        {
            _accounts = accounts;
            for (var row = 0; row < Simulation.SampleRows; row++) for (var col = 0; col < Simulation.SampleColumns; col++)
                Holes.Add(new DrillHole { Number = Holes.Count + 1, X = Simulation.SampleOriginX + (row % 2 == 0 ? col : Simulation.SampleColumns - 1 - col) * Simulation.SamplePitchX, Y = Simulation.SampleOriginY + row * Simulation.SamplePitchY });
            RefreshRecipe();
            Log("INFO", "Machine.MockReady");
            _timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Simulation.TickMilliseconds) };
            _timer.Tick += OnTick; _timer.Start();
        }
        public void RefreshRecipe()
        {
            if (Busy || IsJogging) return;
            _activeRecipe = _accounts.RecipeLibrary.GetActive().Snapshot();
            var p = _activeRecipe.Parameters; RecipeText = $"{p.PowerPercent:0.#} %  /  {p.FrequencyKhz:0.#} kHz  /  {p.PulseCount} pulses";
            Changed();
        }
        private void Authenticate() { if (_accounts.CurrentUser == null) throw new UnauthorizedAccessException(); }
        public void Home()
        {
            Authenticate(); if (!CanHome) return;
            OutputsOff(); State = MachineState.Homing; Homed = false; _tick = 0; LaserOn = false; Log("INFO", "Machine.HomeStarted"); Changed();
        }
        public void Start() => StartCycle(false);
        public void StartDryRun() => StartCycle(true);
        private void StartCycle(bool dryRun)
        {
            Authenticate(); if (!CanStart) return;
            OutputsOff(); IsDryRun = dryRun; ShutterOpen = !dryRun;
            RefreshRecipe(); foreach (var hole in Holes) { if (!dryRun) hole.Done = false; hole.Visited = false; }
            Completed = 0; Elapsed = 0; _index = 0; _tick = 0; _fromX = X; _fromY = Y;
            State = MachineState.Running; Log("INFO", dryRun ? "Machine.DryStarted" : "Machine.CycleStarted"); Changed();
        }
        public void PauseResume()
        {
            Authenticate();
            if (State == MachineState.Running) { ShutterOpen = false; State = MachineState.Paused; LaserOn = false; AirOn = false; Log("INFO", "Machine.CyclePaused"); }
            else if (State == MachineState.Paused && DoorClosed && !EmergencyLatched) { ShutterOpen = !IsDryRun; State = MachineState.Running; Log("INFO", "Machine.CycleResumed"); }
            Changed();
        }
        public void Stop()
        {
            Authenticate(); var wasBusy = Busy; EndJog(); OutputsOff(); if (!wasBusy) { Changed(); return; }
            if (State == MachineState.Homing) Homed = false;
            State = MachineState.Stopped; LaserOn = false; AirOn = false; Log("WARN", "Machine.CycleStopped"); Changed();
        }
        public void Emergency()
        {
            Authenticate(); EmergencyLatched = true; Fault("Machine.EmergencyFault");
        }
        public void ToggleDoor()
        {
            Authenticate(); DoorClosed = !DoorClosed;
            if (!DoorClosed) Fault("Machine.DoorFault"); else { Log("INFO", "Machine.DoorRestored"); Changed(); }
        }
        private void Fault(string key)
        {
            OutputsOff(); FollowEnabled = false; IsJogging = false; _jogX = _jogY = _jogZ = 0; State = MachineState.Fault; Homed = false; LaserOn = false; AirOn = false; Log("ALARM", key); Changed();
        }
        public void Reset()
        {
            Authenticate(); if (State != MachineState.Fault || !DoorClosed) return;
            EmergencyLatched = false; State = MachineState.Idle; LaserOn = false; Log("INFO", "Machine.ResetDone"); Changed();
        }
        public void Jog(string axis)
        {
            Authenticate(); if (!CanStart) return;
            switch (axis) { case "X+": X = Math.Min(100, X + 1); break; case "X-": X = Math.Max(0, X - 1); break; case "Y+": Y = Math.Min(80, Y + 1); break; case "Y-": Y = Math.Max(0, Y - 1); break; }
            Changed();
        }
        public void BeginJog(double x, double y)
        {
            Authenticate();
            if (!CanJog || !double.IsFinite(x) || !double.IsFinite(y) || (x == 0 && y == 0)) return;
            var length = Math.Sqrt(x*x + y*y);
            OutputsOff(); _jogZ = 0; _jogX = x / length; _jogY = y / length; IsJogging = true; LaserOn = false; AirOn = false; Changed();
        }
        public void EndJog() { IsJogging = false; _jogX = _jogY = _jogZ = 0; Changed(); }
        public void EditHole(DrillHole selected, double x, double y, double diameter, double depth)
        {
            if (!CanEdit) throw new UnauthorizedAccessException();
            if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(diameter) || !double.IsFinite(depth) || diameter <= 0 || diameter > 5 || depth <= 0 || depth > 10 || x < diameter/2 || x > 100-diameter/2 || y < diameter/2 || y > 80-diameter/2)
                throw new ArgumentException("Machine.InvalidHole");
            var hole = new DrillHole { Number = selected?.Number ?? Holes.Count + 1, X = x, Y = y, Diameter = diameter, Depth = depth };
            if (selected == null) { if (Holes.Count >= 200) throw new ArgumentException("Machine.HoleLimit"); Holes.Add(hole); }
            else { var index = Holes.IndexOf(selected); if (index < 0) return; Holes[index] = hole; }
            ResetProgress(); Log("INFO", "Machine.GeometryChanged"); Changed();
        }
        public void DeleteHole(DrillHole hole)
        {
            if (!CanEdit) throw new UnauthorizedAccessException();
            if (hole == null) return; Holes.Remove(hole); for (var i = 0; i < Holes.Count; i++) Holes[i].Number = i + 1;
            ResetProgress(); Log("INFO", "Machine.GeometryChanged"); Changed();
        }
        private void ResetProgress() { Completed = 0; IsDryRun = false; foreach (var h in Holes) { h.Done = false; h.Visited = false; } State = Homed ? MachineState.Ready : MachineState.Idle; }
        private void OnTick(object sender, EventArgs e) => Advance();
        public void Advance()
        {
            if (_accounts.CurrentUser == null) { OutputsOff(); IsJogging = false; LaserOn = false; AirOn = false; if (Busy) State = MachineState.Stopped; Changed(); return; }
            if (IsTesting) { if (--_testTicks == 0) OutputsOff(); Changed(); return; }
            if (IsFraming)
            {
                var target = _frame[_frameIndex]; var dx = target.X-X; var dy = target.Y-Y; var distance = Math.Sqrt(dx*dx+dy*dy); var step = Simulation.FrameSpeed*Simulation.TickMilliseconds/1000;
                if (distance <= step) { X = target.X; Y = target.Y; if (++_frameIndex == _frame.Length) { OutputsOff(); Log("INFO", "Machine.FrameDone"); } }
                else { X += dx/distance*step; Y += dy/distance*step; }
                Changed(); return;
            }
            if (IsJogging)
            {
                if (!CanJog) { EndJog(); return; }
                X = Math.Clamp(X + _jogX * Simulation.JogSpeedMmPerSecond * JogOverride / 100 * Simulation.TickMilliseconds / 1000, 0, Simulation.TravelXmm); Y = Math.Clamp(Y + _jogY * Simulation.JogSpeedMmPerSecond * JogOverride / 100 * Simulation.TickMilliseconds / 1000, 0, Simulation.TravelYmm);
                Z = Math.Clamp(Z + _jogZ * Simulation.JogSpeedMmPerSecond * JogOverride / 100 * Simulation.TickMilliseconds / 1000, Simulation.ZMinimum, Simulation.ZMaximum);
                Changed(); return;
            }
            if (State == MachineState.Homing)
            {
                if (++_tick >= Simulation.HomingTicks) { X = Y = Z = 0; Homed = true; State = MachineState.Ready; Log("INFO", "Machine.HomeDone"); }
                Changed(); return;
            }
            if (State != MachineState.Running) return;
            Elapsed += Simulation.TickMilliseconds / 1000.0; _tick++;
            var h = Holes[_index]; var fraction = Math.Min(1, (double)_tick / Simulation.TravelTicks);
            X = _fromX + (h.X - _fromX) * fraction; Y = _fromY + (h.Y - _fromY) * fraction;
            LaserOn = ShutterOpen && !IsDryRun && _tick > Simulation.TravelTicks; AirOn = !IsDryRun && _tick > Simulation.TravelTicks; if (FollowEnabled) Z = Math.Clamp(_activeRecipe.FocusOffsetMm, Simulation.ZMinimum, Simulation.ZMaximum);
            if (_tick >= Simulation.TravelTicks + 1 + Math.Ceiling(_activeRecipe.DwellMilliseconds / Simulation.TickMilliseconds))
            {
                if (IsDryRun) h.Visited = true; else h.Done = true;
                Completed++; LaserOn = false; AirOn = false; _tick = 0; _fromX = X; _fromY = Y; _index++;
                if (_index >= Holes.Count) { ShutterOpen = false; State = MachineState.Completed; Log("INFO", IsDryRun ? "Machine.DryDone" : "Machine.CycleDone"); }
            }
            Changed();
        }
        public void Log(string level, string key)
        {
            Events.Insert(0, new MachineEvent(DateTime.Now.ToString("HH:mm:ss"), level, App.Appearance.Text(key)));
            if (Events.Count > 200) Events.RemoveAt(Events.Count - 1);
        }
        public void Changed() { _revision++; RaisePropertyChanged(""); }
        public void Dispose() { OutputsOff(); _timer.Stop(); _timer.Tick -= OnTick; IsJogging = false; LaserOn = false; AirOn = false; }
    }
}
