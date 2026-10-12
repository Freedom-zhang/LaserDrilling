using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LaserDrilling.Hmi.Services
{
    public enum UserRole { Operator, Administrator }
    public sealed record UserIdentity(string UserName, UserRole Role);
    public interface IAccountService
    {
        IRecipeFileService RecipeLibrary { get; }
        bool NeedsSetup { get; }
        UserIdentity CurrentUser { get; }
        Task<bool> SignInAsync(string userName, string password, CancellationToken token);
        Task InitializeAdministratorAsync(string name, string password, CancellationToken token);
        Task CreateUserAsync(string name, string password, UserRole role);
        IReadOnlyList<UserIdentity> GetUsers();
        ProcessParameters GetParameters();
        void SaveParameters(ProcessParameters parameters);
        IReadOnlyList<ProcessRecipe> GetRecipes();
        void SaveRecipe(string name, ProcessParameters parameters);
        void ApplyRecipe(string name);
        void SignOut();
    }

    public sealed class LocalAccountService : IAccountService
    {
        private const int Iterations = 210000;
        private readonly string _path;
        private readonly object _gate = new object();
        private UserIdentity _current;
        private IRecipeFileService _recipes;
        public IRecipeFileService RecipeLibrary { get { lock (_gate) { RequireLogin(); return _recipes ??= new RecipeFileService(Path.GetDirectoryName(_path), () => CurrentUser, Read().Parameters, Read().Recipes); } } }
        public UserIdentity CurrentUser { get { lock (_gate) return _current; } }
        public bool NeedsSetup { get { lock (_gate) return Read().Users.Count == 0; } }
        public LocalAccountService() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LaserDrilling", "accounts.json")) { }
        public LocalAccountService(string path) { _path = path; }

        public Task<bool> SignInAsync(string userName, string password, CancellationToken token) => Task.Run(() =>
        {
            lock (_gate)
            {
                _current = null;
                token.ThrowIfCancellationRequested();
                var user = Read().Users.FirstOrDefault(u => string.Equals(u.Name, userName?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (user == null) return false;
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(user.Salt), Iterations, HashAlgorithmName.SHA256, 32);
                var valid = CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(user.Hash));
                token.ThrowIfCancellationRequested();
                if (valid) _current = new UserIdentity(user.Name, user.Role);
                return valid;
            }
        }, token);

        public Task InitializeAdministratorAsync(string name, string password, CancellationToken token) => Task.Run(() =>
        {
            lock (_gate)
            {
                using var lease = AcquireWriteLock();
                var state = Read();
                if (state.Users.Count != 0) throw new InvalidOperationException("SetupCompleted");
                state.Users.Add(CreateEntry(name, password, UserRole.Administrator));
                token.ThrowIfCancellationRequested();
                Write(state);
            }
        }, token);

        public Task CreateUserAsync(string name, string password, UserRole role) => Task.Run(() =>
        {
            lock (_gate)
            {
                RequireAdmin();
                using var lease = AcquireWriteLock();
                var state = Read();
                if (state.Users.Any(u => string.Equals(u.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("DuplicateUser");
                state.Users.Add(CreateEntry(name, password, role));
                Write(state);
            }
        });
        public IReadOnlyList<UserIdentity> GetUsers()
        {
            lock (_gate) { RequireAdmin(); return Read().Users.Select(u => new UserIdentity(u.Name, u.Role)).ToList(); }
        }
        public ProcessParameters GetParameters()
        {
            return RecipeLibrary.GetActive().Parameters;
        }
        public void SaveParameters(ProcessParameters parameters)
        {
            RequireAdmin(); var active = RecipeLibrary.GetActive();
            var changed = active with { Parameters = parameters, PowerCurve = new[] { new PowerPoint(0, parameters.PowerPercent), new PowerPoint(100, parameters.PowerPercent) } };
            RecipeLibrary.Save(changed); RecipeLibrary.Apply(changed.Name);
        }
        public void SignOut() { lock (_gate) _current = null; }
        public IReadOnlyList<ProcessRecipe> GetRecipes() => RecipeLibrary.LoadAll();
        public void SaveRecipe(string name, ProcessParameters parameters) => RecipeLibrary.Save(ProcessRecipe.Template(name) with { Parameters = parameters, PowerCurve = new[] { new PowerPoint(0, parameters.PowerPercent), new PowerPoint(100, parameters.PowerPercent) } });
        public void ApplyRecipe(string name) => RecipeLibrary.Apply(name);
        private void RequireLogin() { if (_current == null) throw new UnauthorizedAccessException("AccessDenied"); }
        private void RequireAdmin() { if (_current?.Role != UserRole.Administrator) throw new UnauthorizedAccessException("AccessDenied"); }
        private static AccountEntry CreateEntry(string name, string password, UserRole role)
        {
            name = name?.Trim();
            if (name == null || !Regex.IsMatch(name, "^[a-zA-Z0-9_.-]{3,32}$")) throw new ArgumentException("InvalidUserName");
            if (password == null || password.Length < 8 || password.Length > 128) throw new ArgumentException("InvalidPassword");
            if (!Enum.IsDefined(typeof(UserRole), role)) throw new ArgumentException("AccessDenied");
            var salt = RandomNumberGenerator.GetBytes(16);
            return new AccountEntry { Name = name, Role = role, Salt = Convert.ToBase64String(salt), Hash = Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32)) };
        }
        private AccountStore Read()
        {
            if (!File.Exists(_path)) return new AccountStore();
            // Fail closed: a damaged store must never silently reopen administrator setup.
            var state = JsonSerializer.Deserialize<AccountStore>(File.ReadAllText(_path));
            if (state == null || state.Users == null || state.Users.Count == 0 || state.Parameters == null ||
                state.Users.Any(u => u == null || string.IsNullOrEmpty(u.Name) || string.IsNullOrEmpty(u.Hash) || string.IsNullOrEmpty(u.Salt) || !Enum.IsDefined(typeof(UserRole), u.Role)))
                throw new InvalidDataException("Invalid account store.");
            return state;
        }
        private FileStream AcquireWriteLock()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            return new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        private void Write(AccountStore state)
        {
            var temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllText(temp, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true })); File.Move(temp, _path, true); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        public sealed class AccountStore
        {
            public List<AccountEntry> Users { get; set; } = new List<AccountEntry>();
            public ProcessParameters Parameters { get; set; } = new ProcessParameters(20, 50, 100);
            public List<ProcessRecipe> Recipes { get; set; } = new List<ProcessRecipe>();
        }
        public sealed class AccountEntry
        {
            public string Name { get; set; }
            public UserRole Role { get; set; }
            public string Salt { get; set; }
            public string Hash { get; set; }
        }
    }
}
