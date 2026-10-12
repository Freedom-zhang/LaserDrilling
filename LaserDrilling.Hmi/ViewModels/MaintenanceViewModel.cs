using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LaserDrilling.Hmi.Services;
using Prism.Commands;
using Prism.Mvvm;

namespace LaserDrilling.Hmi.ViewModels
{
    public sealed class MaintenanceViewModel : BindableBase, IDisposable
    {
        private readonly MockDrillingMachine _machine;
        private readonly IAccountService _accounts;
        private string _note = "", _message = "";
        public string SelectedItem { get; set; } = "LaserService";
        public string Note { get => _note; set => SetProperty(ref _note, value); }
        public bool CanRecord => _accounts.CurrentUser?.Role == UserRole.Administrator && !_machine.Busy && !_machine.IsJogging;
        public string Message => _machine.Maintenance.ErrorKey == "" && _message == "" ? "" : App.Appearance.Text("Machine." + (_machine.Maintenance.ErrorKey != "" ? _machine.Maintenance.ErrorKey : _message == "" ? "MaintenanceHint" : _message));
        public string PowerTime => Time(_machine.Maintenance.Data.PowerSeconds);
        public string EmissionTime => Time(_machine.Maintenance.Data.EmissionSeconds);
        public string MotionTime => Time(_machine.Maintenance.Data.MotionSeconds);
        public string GasTime => Time(_machine.Maintenance.Data.GasSeconds);
        private static string Time(double seconds) => $"{(long)(seconds / 3600):0000}:{(int)(seconds / 60) % 60:00}:{(int)seconds % 60:00}";
        public object[] Schedules => MaintenanceSchedule.Defaults.Select(s => {
            var used = Math.Max(0, _machine.Maintenance.MeterHours(s.Meter) - _machine.Maintenance.Data.LastServiceHours.GetValueOrDefault(s.Key));
            return (object)new { Name = App.Appearance.Text("Machine." + s.Key), Interval = s.IntervalHours.ToString("F0") + " h", Used = used.ToString("F2") + " h", Remaining = Math.Max(0, s.IntervalHours-used).ToString("F2") + " h", Status = App.Appearance.Text("Machine." + (used >= s.IntervalHours ? "MaintenanceDue" : "MaintenanceNormal")) };
        }).ToArray();
        public object[] Records => _machine.Maintenance.Data.Records.Select(r => (object)new { Time = r.Time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"), Name = App.Appearance.Text("Machine." + r.Item), r.User, r.Note, Meter = r.MeterHours.ToString("F2") + " h" }).ToArray();
        public DelegateCommand RecordCommand { get; }
        public MaintenanceViewModel(IAccountService accounts, MockDrillingMachine machine)
        {
            _accounts = accounts; _machine = machine;
            RecordCommand = new DelegateCommand(Record, () => CanRecord);
            machine.PropertyChanged += Refresh;
        }
        private void Record()
        {
            try { _machine.Maintenance.Record(SelectedItem, Note, _machine.Busy || _machine.IsJogging); Note = ""; _message = "MaintenanceSaved"; }
            catch (UnauthorizedAccessException) { _message = "MaintenanceDenied"; }
            catch (ArgumentException) { _message = "MaintenanceNoteRequired"; }
            catch (InvalidOperationException) { _message = "MaintenanceLoadError"; }
            catch (System.IO.IOException) { _message = "MaintenanceSaveError"; }
            Refresh(this, null);
        }
        private void Refresh(object sender, PropertyChangedEventArgs e) { RaisePropertyChanged(""); RecordCommand.RaiseCanExecuteChanged(); }
        public void Dispose() => _machine.PropertyChanged -= Refresh;
    }
}
