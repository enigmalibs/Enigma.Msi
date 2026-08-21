using System.Threading.Tasks;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// Opens a URL in whatever the machine considers its browser.
/// </summary>
/// <remarks>
/// A seam for the same reason <see cref="IPathPickerService"/> and <see cref="IUiDispatcher"/> are: a
/// ViewModel that launched a process directly could not be tested without actually opening a browser.
/// </remarks>
public interface IUrlLauncherService
{
    /// <summary>Asks the shell to open <paramref name="url"/>.</summary>
    /// <param name="url">The absolute URL to open.</param>
    /// <returns>
    /// <see langword="true"/> when the shell accepted it; <see langword="false"/> when there is nothing
    /// registered to open it or the shell refused. Never throws for those cases — a machine with no
    /// browser association is the environment's problem, not an app fault.
    /// </returns>
    Task<bool> LaunchAsync(string url);
}
