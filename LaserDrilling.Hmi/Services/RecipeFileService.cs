using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LaserDrilling.Hmi.Services
{
    public interface IRecipeFileService
    {
        string DirectoryPath { get; }
        IReadOnlyList<string> LoadErrors { get; }
        IReadOnlyList<ProcessRecipe> LoadAll();
        ProcessRecipe GetActive();
        void Save(ProcessRecipe recipe);
        void Apply(string name);
    }

    public sealed class RecipeFileService : IRecipeFileService
    {
        private readonly Func<UserIdentity> _user;
        private readonly ProcessParameters _legacy;
        private readonly ProcessRecipe[] _legacyRecipes;
        private readonly object _gate = new();
        private readonly List<string> _errors = new();
        private readonly string _activePath;
        public string DirectoryPath { get; }
        public IReadOnlyList<string> LoadErrors => _errors.ToArray();
        public RecipeFileService(string dataDirectory, Func<UserIdentity> user, ProcessParameters legacy, IEnumerable<ProcessRecipe> legacyRecipes)
        {
            DirectoryPath = Path.Combine(dataDirectory, "Recipes");
            _activePath = Path.Combine(dataDirectory, "active-recipe.json");
            _user = user; _legacy = legacy; _legacyRecipes = legacyRecipes?.ToArray() ?? Array.Empty<ProcessRecipe>();
        }
        private void Authorize(bool write = false)
        {
            if (_user() == null || (write && _user().Role != UserRole.Administrator)) throw new UnauthorizedAccessException("AccessDenied");
        }
        private static ProcessRecipe Legacy(ProcessRecipe recipe) => recipe with { PowerCurve = new[] { new PowerPoint(0, recipe.Parameters.PowerPercent), new PowerPoint(100, recipe.Parameters.PowerPercent) } };
        private void EnsureFiles()
        {
            Directory.CreateDirectory(DirectoryPath);
            if (Directory.EnumerateFiles(DirectoryPath, "*.json").Any()) return;
            // Migrate old embedded recipes once. Never overwrite an existing user file.
            var marker = Path.Combine(DirectoryPath, ".initialized");
            if (!File.Exists(marker) && _legacyRecipes.Length > 0)
                foreach (var recipe in _legacyRecipes) { var migrated = Legacy(recipe); migrated.Validate(); WriteAtomic(PathFor(migrated.Name), migrated); }
            else
            {
                var template = ProcessRecipe.Template();
                if (!File.Exists(marker) && _legacy != null) template = template with { Parameters = _legacy, PowerCurve = template.PowerCurve.Select(p => p with { PowerPercent = p.PowerPercent * _legacy.PowerPercent / 35 }).ToArray() };
                template.Validate(); WriteAtomic(PathFor(template.Name), template);
            }
            File.WriteAllText(marker, "1");
        }
        private string PathFor(string name)
        {
            var safeName = new string(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray()).Trim('.',' ');
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name.ToUpperInvariant()))).Substring(0, 8);
            return Path.Combine(DirectoryPath, "recipe_" + safeName + "_" + hash + ".json");
        }
        public IReadOnlyList<ProcessRecipe> LoadAll()
        {
            lock (_gate)
            {
                Authorize(); EnsureFiles(); _errors.Clear(); var recipes = new List<ProcessRecipe>();
                foreach (var path in Directory.EnumerateFiles(DirectoryPath, "*.json").OrderBy(p => p))
                {
                    try
                    {
                        var recipe = JsonSerializer.Deserialize<ProcessRecipe>(File.ReadAllText(path));
                        if (recipe == null) throw new InvalidDataException(); recipe.Validate();
                        if (recipes.Any(r => string.Equals(r.Name, recipe.Name, StringComparison.OrdinalIgnoreCase))) throw new InvalidDataException();
                        recipes.Add(recipe);
                    }
                    catch (Exception ex) when (ex is JsonException || ex is ArgumentException || ex is IOException) { _errors.Add(Path.GetFileName(path)); }
                }
                return recipes.OrderBy(r => r.Material).ThenBy(r => r.ThicknessMm).ThenBy(r => r.Name).ToArray();
            }
        }
        public ProcessRecipe GetActive()
        {
            lock (_gate)
            {
                Authorize();
                if (File.Exists(_activePath))
                {
                    var active = JsonSerializer.Deserialize<ProcessRecipe>(File.ReadAllText(_activePath));
                    if (active == null) throw new InvalidDataException("Invalid active recipe."); active.Validate(); return active;
                }
                var all = LoadAll();
                var first = all.FirstOrDefault(r => r.Parameters == _legacy) ?? all.FirstOrDefault() ?? throw new InvalidDataException("No valid recipe file.");
                WriteAtomic(_activePath, first); return first.Snapshot();
            }
        }
        public void Save(ProcessRecipe recipe)
        {
            lock (_gate)
            {
                Authorize(true); recipe.Validate(); EnsureFiles();
                // Locate by embedded name so externally renamed JSON files do not produce duplicates.
                var path = Directory.EnumerateFiles(DirectoryPath, "*.json").FirstOrDefault(p =>
                {
                    try { return string.Equals(JsonSerializer.Deserialize<ProcessRecipe>(File.ReadAllText(p))?.Name, recipe.Name, StringComparison.OrdinalIgnoreCase); }
                    catch (JsonException) { return false; }
                }) ?? PathFor(recipe.Name);
                WriteAtomic(path, recipe.Snapshot());
            }
        }
        public void Apply(string name)
        {
            lock (_gate)
            {
                Authorize(true); var recipe = LoadAll().SingleOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException("Machine.InvalidRecipe");
                WriteAtomic(_activePath, recipe.Snapshot());
            }
        }
        private static void WriteAtomic(string path, ProcessRecipe recipe)
        {
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllText(temp, JsonSerializer.Serialize(recipe, new JsonSerializerOptions { WriteIndented = true })); File.Move(temp, path, true); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
