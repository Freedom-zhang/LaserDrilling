using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LaserDrilling.Hmi.Services
{
    public sealed class MaintenanceService
    {
        private readonly IAccountService _accounts;
        private readonly string _path;
        private bool _loadFailed;
        private double _unsavedSeconds;
        public MaintenanceData Data { get; private set; } = new();
        public string ErrorKey { get; private set; } = "";
        public MaintenanceService(IAccountService accounts)
        {
            _accounts = accounts;
            _path = Path.Combine(Path.GetDirectoryName(accounts.RecipeLibrary.DirectoryPath), "maintenance-mock.json");
            try
            {
                if (!File.Exists(_path)) return;
                Data = JsonSerializer.Deserialize<MaintenanceData>(File.ReadAllText(_path)) ?? throw new JsonException();
                if (Data.Records == null || Data.LastServiceHours == null || new[] { Data.PowerSeconds, Data.EmissionSeconds, Data.MotionSeconds, Data.GasSeconds }.Any(v => !double.IsFinite(v) || v < 0) || Data.LastServiceHours.Values.Any(v => !double.IsFinite(v) || v < 0)) throw new JsonException();
                if (Data.Records.Any(r => r == null || !MaintenanceSchedule.Defaults.Any(s => s.Key == r.Item) || r.Note == null || r.User == null || !double.IsFinite(r.MeterHours) || r.MeterHours < 0)) throw new JsonException();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            { Data = new(); _loadFailed = true; ErrorKey = "MaintenanceLoadError"; }
        }
        public void Accumulate(double seconds, bool emission, bool motion, bool gas)
        {
            if (_loadFailed || !double.IsFinite(seconds) || seconds <= 0) return;
            Data.PowerSeconds += seconds;
            if (emission) Data.EmissionSeconds += seconds;
            if (motion) Data.MotionSeconds += seconds;
            if (gas) Data.GasSeconds += seconds;
            _unsavedSeconds += seconds;
            if (_unsavedSeconds >= 10) Save();
        }
        public double MeterHours(string meter) => (meter == "Emission" ? Data.EmissionSeconds : meter == "Motion" ? Data.MotionSeconds : Data.PowerSeconds) / 3600;
        public void Record(string key, string note, bool machineBusy)
        {
            if (_accounts.CurrentUser?.Role != UserRole.Administrator || machineBusy) throw new UnauthorizedAccessException();
            if (_loadFailed) throw new InvalidOperationException("MaintenanceLoadError");
            var schedule = MaintenanceSchedule.Defaults.SingleOrDefault(s => s.Key == key);
            if (schedule == null || string.IsNullOrWhiteSpace(note) || note.Trim().Length > 500) throw new ArgumentException("MaintenanceNoteRequired");
            var old = JsonSerializer.Serialize(Data);
            Data.Records.Insert(0, new(DateTimeOffset.Now, key, _accounts.CurrentUser.UserName, note.Trim(), MeterHours(schedule.Meter)));
            Data.LastServiceHours[key] = MeterHours(schedule.Meter);
            if (!Save()) { Data = JsonSerializer.Deserialize<MaintenanceData>(old); throw new IOException(); }
        }
        public bool Save()
        {
            if (_loadFailed) return false;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                File.WriteAllText(_path + ".tmp", JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true }));
                File.Move(_path + ".tmp", _path, true); _unsavedSeconds = 0; ErrorKey = ""; return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { ErrorKey = "MaintenanceSaveError"; _unsavedSeconds = 0; return false; }
        }
    }
}
