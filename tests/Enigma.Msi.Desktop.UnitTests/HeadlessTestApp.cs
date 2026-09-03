// Using directives stay at file scope, above the namespace declaration: inside a namespace beginning
// with "Enigma.", the simple name "Avalonia" resolves against the referenced Enigma.Avalonia.*
// assemblies instead of Avalonia itself.
using Avalonia;
using Avalonia.Headless;
using Enigma.Msi.Desktop;
using Enigma.Msi.Desktop.UnitTests;

// Every [AvaloniaFact] and [AvaloniaTheory] in this assembly runs on the headless session this
// declares. One session is started for the whole run and shared, so the application below is built
// once.
[assembly: AvaloniaTestApplication(typeof(HeadlessTestApp))]

namespace Enigma.Msi.Desktop.UnitTests;

/// <summary>
/// The headless entry point the Avalonia test session builds: the app's real <see cref="App"/>, on a
/// platform with no screen behind it.
/// </summary>
/// <remarks>
/// <para>
/// The real application object rather than a stand-in, because what these tests assert is what the
/// markup resolves — every colour on the splash is a <c>DynamicResource</c> <c>Enigma*</c> key, and
/// those keys come from the <c>FluentTheme</c> and the <c>Enigma.Avalonia.Desktop</c> dictionary that
/// <c>App.axaml</c> merges, in that order. A copy of that resource surface here would be one more
/// thing to keep in step with <c>App.axaml</c>.
/// </para>
/// <para>
/// Nothing of the app's composition runs: the test session sets up the application without a
/// lifetime, so <c>OnFrameworkInitializationCompleted</c> finds no
/// <c>IClassicDesktopStyleApplicationLifetime</c> and resolves no host, no windows and no services.
/// </para>
/// </remarks>
public static class HeadlessTestApp
{
    /// <summary>
    /// Builds the headless application. Called by <c>AvaloniaTestApplication</c> by convention — keep
    /// it public, static and parameterless.
    /// </summary>
    /// <returns>The configured <see cref="AppBuilder"/>.</returns>
    /// <remarks>
    /// <c>UseHeadless</c> replaces the app's own <c>UsePlatformDetect()</c>; headless drawing is left
    /// on, since these tests read the control tree the markup builds and none of them needs pixels.
    /// </remarks>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .WithInterFont();
}
