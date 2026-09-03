using System.Threading.Tasks;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// Shows the modal About dialog — the app's identity, and the link to its repository.
/// </summary>
/// <remarks>
/// A thin seam over <c>Enigma.Avalonia.Desktop</c>'s <c>IContentDialogService</c>, in the shape of
/// <see cref="IBuildProgressService"/>: the dialog takes a <c>Control</c>, and a ViewModel that
/// constructs controls cannot be tested without standing up an Avalonia application. The window's
/// ViewModel says <em>show About</em>; the card that says it lives on this side of the seam.
/// </remarks>
public interface IAboutDialogService
{
    /// <summary>Shows the dialog and waits for it to be dismissed.</summary>
    /// <returns>A task that completes once the dialog has closed.</returns>
    Task ShowAsync();
}
