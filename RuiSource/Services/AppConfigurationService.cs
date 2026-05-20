using System.Text.Json;
using RuiSource.Models;

namespace RuiSource.Services
{
    public static class AppConfigurationService
    {
        private static readonly string ConfigurationPath = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");

        public static AppConfiguration Load()
        {
            try
            {
                if (!File.Exists(ConfigurationPath))
                {
                    return new AppConfiguration();
                }

                var json = File.ReadAllText(ConfigurationPath);
                return JsonSerializer.Deserialize<AppConfiguration>(json) ?? new AppConfiguration();
            }
            catch
            {
                return new AppConfiguration();
            }
        }

        public static void Save(AppConfiguration configuration)
        {
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigurationPath, json);
        }
    }
}
