using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Schaumamal.Models.DisplayDataResolver;
using Schaumamal.Models.Dumper;
using Schaumamal.Models.Parser;
using Schaumamal.Models.Platform;
using Schaumamal.Models.Repository;
using Schaumamal.ViewModels;
using Schaumamal.ViewModels.Notifications;
using Schaumamal.Views;

namespace Schaumamal;

public partial class App : Application
{
    public static ServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        services.AddSingleton<PlatformInformationProvider>(_ => PlatformInformationProvider.Current());
        services.AddSingleton<NicknameProvider>();
        services.AddSingleton<XmlParser>();
        services.AddSingleton<NotificationManager>();
        services.AddSingleton<AppRepository>();
        services.AddSingleton<DisplayDataResolver>();
        services.AddSingleton<Dumper>();
        services.AddSingleton<AppViewModel>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<AppViewModel>()
            };
            desktop.ShutdownRequested += (_, _) =>
            {
                Services.GetRequiredService<AppViewModel>().Cleanup();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
