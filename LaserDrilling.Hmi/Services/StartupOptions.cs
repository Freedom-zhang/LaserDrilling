using System;
using System.IO;
using System.Text.Json;

namespace LaserDrilling.Hmi.Services
{
    public sealed class StartupOptions
    {
        public string CompanyName { get; set; } = "XX激光";
        public string CompanyEnglishName { get; set; } = "STAR LASER";
        public string ProductName { get; set; } = "激光打孔控制系统";
        public double SplashDurationSeconds { get; set; } = 2;

        public static StartupOptions Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Configuration", "startup.json");
            try
            {
                var options = File.Exists(path)
                    ? JsonSerializer.Deserialize<StartupOptions>(File.ReadAllText(path)) ?? new StartupOptions()
                    : new StartupOptions();
                if (!double.IsFinite(options.SplashDurationSeconds) || options.SplashDurationSeconds < 0 || options.SplashDurationSeconds > 30)
                    options.SplashDurationSeconds = 2;
                if (string.IsNullOrWhiteSpace(options.CompanyName)) options.CompanyName = "XX激光";
                if (string.IsNullOrWhiteSpace(options.CompanyEnglishName)) options.CompanyEnglishName = "STAR LASER";
                if (string.IsNullOrWhiteSpace(options.ProductName)) options.ProductName = "激光打孔控制系统";
                return options;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                return new StartupOptions();
            }
        }
    }
}
