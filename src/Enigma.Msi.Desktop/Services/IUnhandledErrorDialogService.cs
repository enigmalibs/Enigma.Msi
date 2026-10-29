using System.Threading.Tasks;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// Shows the safety net's dialog — "Something went wrong" — for one batch of errors.
/// </summary>
/// <remarks>
/// A thin seam over <c>Enigma.Avalonia.Desktop</c>'s <c>IContentDialogService</c>, in the shape of
/// <see cref="IAboutDialogService"/>, so <see cref="UnhandledErrorReporter"/> can be tested without
/// standing up a window.
/// </remarks>
public interface IUnhandledErrorDialogService
{
    /// <summary>
    /// Waits until the window can show a dialog, shows the batch, and waits for it to be dismissed.
    /// </summary>
    /// <param name="batch">The errors to show; later errors counted into it show while it is open.</param>
    /// <returns>A task that completes once the dialog has closed.</returns>
    Task ShowAsync(UnhandledErrorBatch batch);
}
