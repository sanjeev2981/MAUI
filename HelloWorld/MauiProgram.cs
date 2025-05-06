using Microsoft.Extensions.Logging;
using Plugin.Maui.Biometric;
using HelloWorld.ViewModels;
using HelloWorld.Services;
using HelloWorld.Views;
using Microsoft.Extensions.Logging.Console;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace HelloWorld
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp() // ← REQUIRED for SKCanvasView to work
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            // Add logging level
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Information);

            //Register Services
            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<IBiometric>(BiometricAuthenticationService.Default);
            builder.Services.AddSingleton<ProductService>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddSingleton<StockTickerService>();
            builder.Services.AddSingleton<SingleStockService>();

            // Register ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<StockTickerViewModel>();
            builder.Services.AddTransient<FavoriteStocksViewModel>();
            builder.Services.AddTransient<StockChartViewModel>();

            // Register Views
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<WebPage>();
            builder.Services.AddTransient<StockTickerPage>();
            builder.Services.AddTransient<FavoriteStocksPage>();
            builder.Services.AddTransient<StockChartPage>();

            return builder.Build();
        }
    }
}
