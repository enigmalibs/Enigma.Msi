using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Enigma.Avalonia.Desktop.Services;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// <see cref="IPathPickerService"/> over the Enigma.Avalonia.Desktop picker services.
/// </summary>
public sealed class PathPickerService : IPathPickerService
{
    private static readonly IReadOnlyList<FilePickerFileType> ProfileFileTypes =
    [
        new FilePickerFileType("MSI package profile") { Patterns = ["*.msipkg.json"] },
        new FilePickerFileType("JSON") { Patterns = ["*.json"] }
    ];

    private static readonly IReadOnlyList<FilePickerFileType> IconFileTypes =
    [
        new FilePickerFileType("Icon") { Patterns = ["*.ico"] }
    ];

    private readonly IFileDialogService _fileDialogService;
    private readonly IFolderDialogService _folderDialogService;

    /// <summary>Creates the picker over the two storage-provider-backed dialog services.</summary>
    /// <param name="fileDialogService">Opens and saves files.</param>
    /// <param name="folderDialogService">Opens directories.</param>
    /// <exception cref="ArgumentNullException">Either service is <see langword="null"/>.</exception>
    public PathPickerService(IFileDialogService fileDialogService, IFolderDialogService folderDialogService)
    {
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _folderDialogService = folderDialogService ?? throw new ArgumentNullException(nameof(folderDialogService));
    }

    /// <inheritdoc />
    public async Task<string?> PickProfileToOpenAsync()
    {
        IEnumerable<string> paths = await _fileDialogService
            .ShowOpenFileDialogAsync("Open package profile", false, string.Empty, string.Empty, ProfileFileTypes)
            .ConfigureAwait(true);

        return paths.FirstOrDefault();
    }

    /// <inheritdoc />
    public Task<string?> PickProfileToSaveAsync(string? suggestedFileName)
        => _fileDialogService.ShowSaveFileDialogAsync(
            "Save package profile",
            string.Empty,
            suggestedFileName ?? string.Empty,
            "msipkg.json",
            true,
            ProfileFileTypes);

    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(string title, string? startLocation)
    {
        IEnumerable<string> paths = await _folderDialogService
            .ShowOpenFolderDialogAsync(title, false, ExistingDirectoryOrEmpty(startLocation), string.Empty)
            .ConfigureAwait(true);

        return paths.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<string?> PickIconAsync(string? startLocation)
    {
        IEnumerable<string> paths = await _fileDialogService
            .ShowOpenFileDialogAsync(
                "Select icon", false, ExistingDirectoryOrEmpty(startLocation), string.Empty, IconFileTypes)
            .ConfigureAwait(true);

        return paths.FirstOrDefault();
    }

    // A start location that does not exist makes the picker fall back to its own default anyway; the
    // check keeps a half-typed path from reaching the storage provider as a suggestion.
    private static string ExistingDirectoryOrEmpty(string? path)
        => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) ? path : string.Empty;
}
