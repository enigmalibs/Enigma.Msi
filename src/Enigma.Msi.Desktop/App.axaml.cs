using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Enigma.Avalonia.Desktop.Services;
using Enigma.Msi.Desktop.ViewModels;
using Enigma.Msi.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Enigma.Msi.Desktop;

/// <summary>
/// The Avalonia application. Resolves the windows from the host built in <see cref="Program"/>, hands the
/// control library its three hosts and the window's storage provider, and sequences the splash-to-window
/// handover.
/// </summary>
public partial class App : Application
{
    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        // Under the XAML designer Program.Main() never runs, so AppHost is null: build a throwaway
        // provider rather than crash the previewer.
        IServiceProvider services = Program.AppHost?.Services ?? BuildDesignerServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = services.GetRequiredService<MainWindowViewModel>();

            // Before any window is shown: a service whose host is still unregistered throws the
            // moment a ViewModel asks it for anything.
            services.GetRequiredService<IContentDialogService>().RegisterHost(mainWindow.HostDialog);
            services.GetRequiredService<IOverlayService>().RegisterHost(mainWindow.HostOverlay);
            services.GetRequiredService<IInfoBarService>().RegisterHost(mainWindow.HostInfoBar);
            services.GetRequiredService<IFileDialogService>().SetStorageProvider(mainWindow.StorageProvider);
            services.GetRequiredService<IFolderDialogService>().SetStorageProvider(mainWindow.StorageProvider);

            var splash = services.GetRequiredService<SplashWindow>();
            splash.DataContext = services.GetRequiredService<SplashViewModel>();
            splash.Dismissed += (_, _) => ShowMainWindow(desktop, mainWindow, splash);

            // The lifetime shows whatever MainWindow holds once this method returns, so the splash needs
            // no Show() of its own — and being the initial MainWindow is what makes it the *only* thing
            // on screen, rather than something that appears alongside the real window.
            desktop.MainWindow = splash;
        }

        base.OnFrameworkInitializationCompleted();
    }

    // The order here is load-bearing, though not for the obvious reason: the default ShutdownMode is
    // OnLastWindowClose — a last-window-standing rule, not a MainWindow-identity one — so the real window
    // has to be open before the splash closes, or the process exits. Reassigning MainWindow is not enough
    // on its own either: only the *initial* MainWindow is shown automatically.
    private static void ShowMainWindow(
        IClassicDesktopStyleApplicationLifetime desktop,
        MainWindow mainWindow,
        SplashWindow splash)
    {
        desktop.MainWindow = mainWindow;
        mainWindow.Show();
        splash.Close();
    }

    private static IServiceProvider BuildDesignerServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddNLog();
        });
        services.AddEnigmaAvaloniaServices();
        services.AddMsiServices();
        services.AddViewsAndViewModels();

        return services.BuildServiceProvider();
    }
}
