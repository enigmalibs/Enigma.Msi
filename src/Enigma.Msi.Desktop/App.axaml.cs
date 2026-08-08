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
/// The Avalonia application. Resolves the window from the host built in <see cref="Program"/> and
/// hands the control library its three hosts and the window's storage provider.
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

            // Before the window is shown: a service whose host is still unregistered throws the
            // moment a ViewModel asks it for anything.
            services.GetRequiredService<IContentDialogService>().RegisterHost(mainWindow.HostDialog);
            services.GetRequiredService<IOverlayService>().RegisterHost(mainWindow.HostOverlay);
            services.GetRequiredService<IInfoBarService>().RegisterHost(mainWindow.HostInfoBar);
            services.GetRequiredService<IFileDialogService>().SetStorageProvider(mainWindow.StorageProvider);
            services.GetRequiredService<IFolderDialogService>().SetStorageProvider(mainWindow.StorageProvider);

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
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
