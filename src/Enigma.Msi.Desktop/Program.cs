using System;
using System.Threading;
using Avalonia;
using Enigma.Msi.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Enigma.Msi.Desktop;

internal sealed class Program
{
    /// <summary>
    /// The composition root, exposed so <see cref="App"/> can resolve the window and its ViewModel.
    /// <see langword="null"/> only under the XAML designer, which never runs <see cref="Main"/>.
    /// </summary>
    internal static IHost? AppHost { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        // First: anything that fails from here on — the host, the platform, a task nobody awaits — reaches
        // the safety net rather than ending the process unrecorded.
        UnhandledExceptionHooks.Install();

        try
        {
            Run(args);
        }
        catch (Exception exception)
        {
            // What escapes here is a failure to start or a crash the dispatcher hook could not catch. The
            // safety net cannot show a dialog without a window, and a WinExe has no console: the log file
            // is the only place left to say it.
            UnhandledExceptionHooks.Detach();
            NLog.LogManager.GetCurrentClassLogger().Fatal(exception, "The application ended on an unhandled exception");
            NLog.LogManager.Flush();
            throw;
        }
    }

    private static void Run(string[] args)
    {
        AppHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddNLog();
                });
                services.AddEnigmaAvaloniaServices();
                services.AddMsiServices();
                services.AddViewsAndViewModels();
            })
            .Build();

        // Started before Avalonia so anything resolved during framework initialization — the window,
        // its ViewModel, the logger they use — is already available.
        AppHost.Start();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        // Before the container goes: a late report must not log through a disposed logger.
        UnhandledExceptionHooks.Detach();

        // Clearing the Avalonia SynchronizationContext first: StopAsync's continuations would
        // otherwise be posted back to a dispatcher that has already shut down, and deadlock.
        SynchronizationContext.SetSynchronizationContext(null);
        AppHost.StopAsync().GetAwaiter().GetResult();
        AppHost.Dispose();
    }

    // Avalonia configuration, don't remove; also used by the visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
