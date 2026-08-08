using System.Threading.Tasks;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// The four path questions this app asks the user: which profile to open, where to save one, which
/// folder to package or write to, and which icon file to use. Each answers with a plain path, or
/// <see langword="null"/> when the user cancelled.
/// </summary>
/// <remarks>
/// A deliberate thin seam over <c>Enigma.Avalonia.Desktop</c>'s <c>IFileDialogService</c> and
/// <c>IFolderDialogService</c>. Their path-returning convenience overloads are C# 14
/// <c>extension</c> members — static, and therefore impossible to substitute in a test — while the
/// interface methods themselves traffic in <c>IStorageFile</c>/<c>IStorageFolder</c>, which a
/// ViewModel test has no business faking. This interface is what the ViewModels depend on, so
/// picking a path is mockable without either problem.
/// </remarks>
public interface IPathPickerService
{
    /// <summary>Asks for an existing <c>.msipkg.json</c> profile to open.</summary>
    /// <returns>The chosen file's path, or <see langword="null"/> if the user cancelled.</returns>
    Task<string?> PickProfileToOpenAsync();

    /// <summary>Asks where to write a <c>.msipkg.json</c> profile.</summary>
    /// <param name="suggestedFileName">File name to pre-fill, extension included.</param>
    /// <returns>The chosen file's path, or <see langword="null"/> if the user cancelled.</returns>
    Task<string?> PickProfileToSaveAsync(string? suggestedFileName);

    /// <summary>Asks for a directory.</summary>
    /// <param name="title">Dialog title, saying which directory is being asked for.</param>
    /// <param name="startLocation">Directory to open the dialog at; ignored when blank or missing.</param>
    /// <returns>The chosen directory's path, or <see langword="null"/> if the user cancelled.</returns>
    Task<string?> PickFolderAsync(string title, string? startLocation);

    /// <summary>Asks for an icon file.</summary>
    /// <param name="startLocation">Directory to open the dialog at; ignored when blank or missing.</param>
    /// <returns>The chosen file's path, or <see langword="null"/> if the user cancelled.</returns>
    Task<string?> PickIconAsync(string? startLocation);
}
