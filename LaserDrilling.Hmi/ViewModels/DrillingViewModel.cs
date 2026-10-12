using System;
using System.ComponentModel;
using System.Globalization;
using LaserDrilling.Hmi.Services;
using Prism.Commands;
using Prism.Mvvm;

namespace LaserDrilling.Hmi.ViewModels
{
    /// <summary>
    /// 协调加工命令、操作确认、孔位编辑和加工设定，调用设备模拟服务。
    /// </summary>
    public sealed class DrillingViewModel : BindableBase, IDisposable
    {
        public MockDrillingMachine Machine { get; }
        public ProcessSetpoints Setpoints { get; } = new();
        private bool _loadingSetpoints, _setpointsDirty;
        private ProcessParameters _loadedParameters;
        public DelegateCommand ApplySetpointsCommand { get; }
        public DelegateCommand ReloadSetpointsCommand { get; }
        public string SetpointHint => App.Appearance.Text("Machine." + (_setpointsDirty ? "SetpointsDraft" : "SetpointsApplied"));
        private void LoadSetpoints()
        {
            _loadingSetpoints = true; _loadedParameters = Machine.ConfiguredParameters;
            Setpoints.Power = _loadedParameters.PowerPercent; Setpoints.Frequency = _loadedParameters.FrequencyKhz; Setpoints.Pulses = _loadedParameters.PulseCount; Setpoints.Cycles = Machine.ConfiguredCycles;
            _loadingSetpoints = false; _setpointsDirty = false; RaisePropertyChanged(nameof(SetpointHint)); ApplySetpointsCommand?.RaiseCanExecuteChanged();
        }
        private void SetpointsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_loadingSetpoints) return;
            _setpointsDirty = true; RaisePropertyChanged(nameof(SetpointHint)); ApplySetpointsCommand.RaiseCanExecuteChanged();
        }
        private void ApplySetpoints()
        {
            try { Machine.ApplySetpoints(Setpoints.ToParameters(), Setpoints.Cycles); LoadSetpoints(); }
            catch (UnauthorizedAccessException) { _notice = "AccessDenied"; }
            catch (ArgumentException) { _notice = "Machine.InvalidRecipe"; }
            RaisePropertyChanged(nameof(Notice));
        }
        private DrillHole _selected;
        public DrillHole Selected { get => _selected; set { if (SetProperty(ref _selected, value) && value != null) { HoleX = value.X.ToString(CultureInfo.InvariantCulture); HoleY = value.Y.ToString(CultureInfo.InvariantCulture); Diameter = value.Diameter.ToString(CultureInfo.InvariantCulture); Depth = value.Depth.ToString(CultureInfo.InvariantCulture); RaisePropertyChanged(""); Machine.Changed(); } } }
        public string HoleX { get; set; } = "50";
        public string HoleY { get; set; } = "40";
        public string Diameter { get; set; } = "0.5";
        public string Depth { get; set; } = "0.8";
        private string _pending = "", _notice = "";
        public bool HasConfirmation => _pending != "";
        public string Confirmation => HasConfirmation ? App.Appearance.Text("Machine.Confirm" + _pending) : "";
        public string Notice => _notice == "" ? "" : App.Appearance.Text(_notice);
        public string ShutterCaption => App.Appearance.Text("Machine." + (Machine.ShutterOpen ? "ShutterOnCaption" : "ShutterOffCaption"));
        public string FollowCaption => App.Appearance.Text("Machine." + (Machine.FollowEnabled ? "FollowOnCaption" : "FollowOffCaption"));
        private string AuxiliaryBlock => !Machine.DoorClosed || Machine.EmergencyLatched || Machine.State == MachineState.Fault ? "AuxFaultHint" : Machine.Busy || Machine.IsJogging ? "AuxBusyHint" : null;
        public string ShutterHint => App.Appearance.Text("Machine." + (AuxiliaryBlock ?? (Machine.ShutterOpen ? "ShutterOpened" : "ShutterClosed")));
        public string FollowHint => AuxiliaryBlock != null ? App.Appearance.Text("Machine." + AuxiliaryBlock) : !Machine.Homed ? App.Appearance.Text("Machine.FollowHomeHint") : string.Format(App.Appearance.Text("Machine.FollowTargetHint"), Machine.FollowTargetZ);
        public string PauseCaption => App.Appearance.Text(Machine.State == MachineState.Paused ? "Machine.Resume" : "Machine.Pause");
        public DelegateCommand ShutterCommand { get; }
        public DelegateCommand RedLightCommand { get; }
        public DelegateCommand AirCommand { get; }
        public DelegateCommand FollowCommand { get; }
        public DelegateCommand EmitCommand { get; }
        public DelegateCommand FrameCommand { get; }
        public DelegateCommand HomeCommand { get; }
        public DelegateCommand StartCommand { get; }
        public DelegateCommand DryRunCommand { get; }
        public DelegateCommand PauseCommand { get; }
        public DelegateCommand StopCommand { get; }
        public DelegateCommand EmergencyCommand { get; }
        public DelegateCommand DoorCommand { get; }
        public DelegateCommand ResetCommand { get; }
        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand<string> JogCommand { get; }
        public DelegateCommand AddCommand { get; }
        public DelegateCommand ApplyCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public DrillingViewModel(IAccountService accounts)
        {
            Machine = new MockDrillingMachine(accounts);
            ApplySetpointsCommand = new DelegateCommand(ApplySetpoints, () => Machine.CanEdit && _setpointsDirty);
            ReloadSetpointsCommand = new DelegateCommand(LoadSetpoints);
            LoadSetpoints(); Setpoints.PropertyChanged += SetpointsChanged;
            ShutterCommand = new DelegateCommand(Machine.ToggleShutter, () => Machine.CanAuxiliary);
            RedLightCommand = new DelegateCommand(Machine.ToggleRedLight, () => Machine.CanAuxiliary);
            AirCommand = new DelegateCommand(Machine.ToggleAir, () => Machine.CanAuxiliary);
            FollowCommand = new DelegateCommand(Machine.ToggleFollow, () => Machine.CanAuxiliary && Machine.Homed);
            EmitCommand = new DelegateCommand(() => Ask("Emit"), () => Machine.CanTest);
            FrameCommand = new DelegateCommand(() => Ask("Frame"), () => Machine.CanStart);
            HomeCommand = new DelegateCommand(() => Ask("Home"), () => Machine.CanHome);
            StartCommand = new DelegateCommand(() => Ask("Start"), () => Machine.CanStart);
            DryRunCommand = new DelegateCommand(() => Ask("DryRun"), () => Machine.CanStart);
            PauseCommand = new DelegateCommand(Machine.PauseResume, () => Machine.State == MachineState.Running || Machine.State == MachineState.Paused);
            StopCommand = new DelegateCommand(Machine.Stop, () => Machine.Busy || Machine.IsJogging || Machine.HasOutputs);
            EmergencyCommand = new DelegateCommand(() => { Cancel(); Machine.Emergency(); });
            DoorCommand = new DelegateCommand(() => { Cancel(); Machine.ToggleDoor(); });
            ResetCommand = new DelegateCommand(() => Ask("Reset"), () => Machine.State == MachineState.Fault && Machine.DoorClosed);
            ConfirmCommand = new DelegateCommand(() => { var action = _pending; Cancel(); if (action == "Home") Machine.Home(); else if (action == "Start") Machine.Start(); else if (action == "DryRun") Machine.StartDryRun(); else if (action == "Emit") Machine.TestEmission(); else if (action == "Frame") Machine.Frame(); else if (action == "Reset") Machine.Reset(); });
            CancelCommand = new DelegateCommand(Cancel);
            JogCommand = new DelegateCommand<string>(Machine.Jog, _ => Machine.CanStart);
            AddCommand = new DelegateCommand(() => Edit(null), () => Machine.CanEdit);
            ApplyCommand = new DelegateCommand(() => Edit(Selected), () => Machine.CanEdit && Selected != null);
            DeleteCommand = new DelegateCommand(() => { Machine.DeleteHole(Selected); Selected = Machine.Holes.Count > 0 ? Machine.Holes[0] : null; }, () => Machine.CanEdit && Selected != null);
            Machine.PropertyChanged += OnMachineChanged;
            Selected = Machine.Holes[0];
        }
        private void Ask(string action) { _pending = action; RaisePropertyChanged(nameof(HasConfirmation)); RaisePropertyChanged(nameof(Confirmation)); }
        private void Cancel() { _pending = ""; RaisePropertyChanged(nameof(HasConfirmation)); }
        private void Edit(DrillHole selected)
        {
            try
            {
                Machine.EditHole(selected, double.Parse(HoleX, CultureInfo.InvariantCulture), double.Parse(HoleY, CultureInfo.InvariantCulture), double.Parse(Diameter, CultureInfo.InvariantCulture), double.Parse(Depth, CultureInfo.InvariantCulture));
                Selected = selected == null ? Machine.Holes[Machine.Holes.Count-1] : Machine.Holes[selected.Number-1]; _notice = "Machine.GeometryChanged";
            }
            catch (UnauthorizedAccessException) { _notice = "AccessDenied"; }
            catch (ArgumentException ex) { _notice = ex.Message; }
            catch (FormatException) { _notice = "Machine.InvalidHole"; }
            catch (OverflowException) { _notice = "Machine.InvalidHole"; }
            RaisePropertyChanged(nameof(Notice));
        }
        private void OnMachineChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_setpointsDirty && _loadedParameters != Machine.ConfiguredParameters) LoadSetpoints();
            ApplySetpointsCommand.RaiseCanExecuteChanged(); RaisePropertyChanged(nameof(SetpointHint));
            RaisePropertyChanged(nameof(ShutterCaption)); RaisePropertyChanged(nameof(FollowCaption));
            RaisePropertyChanged(nameof(ShutterHint)); RaisePropertyChanged(nameof(FollowHint));
            foreach (var command in new[] { ShutterCommand, RedLightCommand, AirCommand, FollowCommand, EmitCommand, FrameCommand }) command.RaiseCanExecuteChanged();
            DryRunCommand.RaiseCanExecuteChanged();
            HomeCommand.RaiseCanExecuteChanged(); StartCommand.RaiseCanExecuteChanged(); PauseCommand.RaiseCanExecuteChanged(); StopCommand.RaiseCanExecuteChanged(); ResetCommand.RaiseCanExecuteChanged(); JogCommand.RaiseCanExecuteChanged();
            AddCommand.RaiseCanExecuteChanged(); ApplyCommand.RaiseCanExecuteChanged(); DeleteCommand.RaiseCanExecuteChanged(); RaisePropertyChanged(nameof(PauseCaption));
        }
        public void Dispose() { Setpoints.PropertyChanged -= SetpointsChanged; Machine.PropertyChanged -= OnMachineChanged; Machine.Dispose(); }
    }
}
