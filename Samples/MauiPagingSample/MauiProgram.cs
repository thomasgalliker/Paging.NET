using CommunityToolkit.Maui;
using MauiPagingSample.Services;
using MauiPagingSample.ViewModels;
using MauiPagingSample.Views;
using Microsoft.Extensions.Logging;

namespace MauiPagingSample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddLogging(b =>
        {
            b.ClearProviders();
            b.SetMinimumLevel(LogLevel.Trace);
            b.AddDebug();
        });

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddSingleton<ICarService, CarService>();
        builder.Services.AddSingleton<ILauncher>(_ => Launcher.Default);

        return builder.Build();
    }
}