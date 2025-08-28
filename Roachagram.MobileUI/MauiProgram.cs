using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roachagram.MobileUI.Services;

namespace Roachagram.MobileUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
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
            builder.Services.AddSingleton<IRoachagramAPIService, RoachagramAPIService>();

            // Register appsettings.json
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // This requires the Microsoft.Extensions.Configuration.Json package and using directive
                .Build();
            builder.Configuration.AddConfiguration(configuration);

            return builder.Build();
        }
    }
}
