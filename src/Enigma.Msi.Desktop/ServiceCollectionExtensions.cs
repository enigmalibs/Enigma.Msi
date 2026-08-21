using Enigma.Avalonia.Desktop.Services;
using Enigma.Msi.Build;
using Enigma.Msi.Desktop.Services;
using Enigma.Msi.Desktop.ViewModels;
using Enigma.Msi.Desktop.Views;
using Enigma.Msi.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Enigma.Msi.Desktop;

/// <summary>
/// The app's composition, split by origin: the control library's services, the MSI library's, and this
/// app's own views and ViewModels.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the Enigma.Avalonia.Desktop services this app uses, as singletons — the lifetime
        /// they require, since three of them own a host control registered once at startup.
        /// </summary>
        /// <remarks>
        /// <c>INavigationService</c> is deliberately absent: this app is a single form, not a paged
        /// shell, so nothing would ever navigate.
        /// </remarks>
        public void AddEnigmaAvaloniaServices()
        {
            _ = services.AddSingleton<IFileDialogService, FileDialogService>();
            _ = services.AddSingleton<IFolderDialogService, FolderDialogService>();
            _ = services.AddSingleton<IContentDialogService, ContentDialogService>();
            _ = services.AddSingleton<IInfoBarService, InfoBarService>();
            _ = services.AddSingleton<IOverlayService, OverlayService>();
        }

        /// <summary>
        /// Registers the MSI library's services plus this app's five thin seams over the UI framework.
        /// </summary>
        /// <remarks>
        /// The library ships no <c>AddEnigmaMsi()</c> extension of its own — registration is the
        /// consumer's choice — so the wiring lives here. <c>MsiBuildService</c>'s default options are
        /// what this app wants: the worker sits in <c>worker/</c> beside the executable, which is
        /// exactly where <c>build/CopyWorkerOutput.targets</c> puts it.
        /// </remarks>
        public void AddMsiServices()
        {
            _ = services.AddSingleton<IMsiPackageValidator, MsiPackageValidator>();
            _ = services.AddSingleton<IMsiBuildService>(_ => new MsiBuildService());
            _ = services.AddSingleton<IPathPickerService, PathPickerService>();
            _ = services.AddSingleton<IUiDispatcher, UiDispatcher>();
            _ = services.AddSingleton<IBuildProgressService, BuildProgressService>();
            _ = services.AddSingleton<IUrlLauncherService, UrlLauncherService>();
            _ = services.AddSingleton<IAboutDialogService, AboutDialogService>();
        }

        /// <summary>Registers the windows, the form, the dialog content and their ViewModels.</summary>
        /// <remarks>
        /// The splash pair is transient on purpose: it is shown once at startup and closed, and a
        /// singleton would keep a dead window alive for the life of the process.
        /// </remarks>
        public void AddViewsAndViewModels()
        {
            _ = services.AddSingleton<MainWindow>();
            _ = services.AddSingleton<MainWindowViewModel>();
            _ = services.AddSingleton<PackageEditorViewModel>();
            _ = services.AddSingleton<AboutViewModel>();
            _ = services.AddTransient<SplashWindow>();
            _ = services.AddTransient<SplashViewModel>();
        }
    }
}
