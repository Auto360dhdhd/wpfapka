using MediWait.Wpf.Data;
using MediWait.Wpf.Sluzby;
using MediWait.Wpf.ViewModely;

namespace MediWait.Wpf;

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

        builder.Services.AddSingleton<DatabazeSluzba>();
        builder.Services.AddSingleton<SenzorovaSluzba>();
        builder.Services.AddSingleton<HlavniViewModel>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
