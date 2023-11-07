using Config.Net;
using System;
using System.IO;

namespace ConfidentialityDocSearcher.Model.AppSettings.AppConfig
{
    public interface IAppConfig
    {
    }

    public static class AppConfig
    {
        internal static IAppConfig GetConfigFromDefaultPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configPath = Path.Combine(appDataPath, $"{AppConst.Get().AppName}", "settings.json");
            return new ConfigurationBuilder<IAppConfig>()
                .UseJsonFile(configPath)
                .Build();
        }
    }
}