using System.Threading.Tasks;
using System.Windows.Input;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// Shows and updates the modal card that covers the window while an MSI build runs, so a build that
/// takes minutes is visibly alive and cancellable.
/// </summary>
/// <remarks>
/// A thin seam over <c>Enigma.Avalonia.Desktop</c>'s <c>IOverlayService</c>, for the same reason
/// <see cref="IPathPickerService"/> and <see cref="IUiDispatcher"/> exist: the overlay takes a
/// <c>Control</c>, and a ViewModel that constructs controls cannot be tested without standing up an
/// Avalonia application. The ViewModel says <em>show me building</em>; the card that says it lives on
/// this side of the seam.
/// </remarks>
public interface IBuildProgressService
{
    /// <summary>Shows the card, dimming the window behind it.</summary>
    /// <param name="cancelCommand">
    /// What the card's Cancel button invokes — the one cancel affordance while the overlay is up.
    /// </param>
    /// <returns>A task that completes once the card is on screen.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <paramref name="cancelCommand"/> is <see langword="null"/>.
    /// </exception>
    Task ShowAsync(ICommand cancelCommand);

    /// <summary>
    /// Replaces the card's message line with the latest thing the build said. A no-op while the card is
    /// not shown, so a line that arrives late costs nothing.
    /// </summary>
    /// <param name="message">The line to show.</param>
    void ReportMessage(string message);

    /// <summary>
    /// Hides the card. Idempotent: hiding what is not shown is a no-op, which is what lets a caller hide
    /// it on the path it took <em>and</em> guarantee it in a <c>finally</c>.
    /// </summary>
    /// <returns>A task that completes once the card is gone.</returns>
    Task HideAsync();
}
