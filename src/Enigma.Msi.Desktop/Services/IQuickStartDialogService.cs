using System.Threading.Tasks;
using Enigma.Msi.Desktop.ViewModels;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// Shows the modal quick-start dialog and reports what the user answered.
/// </summary>
/// <remarks>
/// A thin seam over <c>Enigma.Avalonia.Desktop</c>'s <c>IContentDialogService</c>, in the shape of
/// <see cref="IAboutDialogService"/> and <see cref="IBuildProgressService"/>: the dialog takes a
/// <c>Control</c>, and a ViewModel that constructs controls cannot be tested without standing up an
/// Avalonia application. The window's ViewModel asks for six answers; the card that collects them lives
/// on this side of the seam.
/// </remarks>
public interface IQuickStartDialogService
{
    /// <summary>Shows the dialog and waits for it to be dismissed.</summary>
    /// <returns>
    /// The answers when the user pressed Apply on a complete set, or <see langword="null"/> when the
    /// dialog was cancelled, dismissed, or closed with the answers still incomplete.
    /// </returns>
    Task<QuickStartSettings?> ShowAsync();
}
