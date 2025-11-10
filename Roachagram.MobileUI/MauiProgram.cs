using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Roachagram.MobileUI.Services;

namespace Roachagram.MobileUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            string appSettingsContent;
            using (var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").Result)
            using (var reader = new StreamReader(stream))
            {
                appSettingsContent = reader.ReadToEnd();
            }

            // Parse the JSON content into a configuration object
            var appSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(appSettingsContent);

            if (appSettings != null)
            {
                builder.Configuration.AddInMemoryCollection(appSettings.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)));
            }

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Register services
            builder.Services.AddScoped<HttpClient>();
            builder.Services.AddSingleton<IRemoteTelemetryService, RemoteTelemetryService>();
            builder.Services.AddSingleton<IRoachagramAPIService, RoachagramAPIService>();
            return builder.Build();
        }
    }
}