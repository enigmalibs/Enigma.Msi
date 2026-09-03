using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Enigma.Msi.Desktop.Services;

/// <summary>
/// <see cref="IUrlLauncherService"/> over the shell: <see cref="Process.Start(ProcessStartInfo)"/> with
/// <see cref="ProcessStartInfo.UseShellExecute"/>, which is what hands a URL to the default browser.
/// </summary>
public sealed class UrlLauncherService : IUrlLauncherService
{
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is <see langword="null"/>.</exception>
    public Task<bool> LaunchAsync(string url)
    {
        if (url is null)
        {
            throw new ArgumentNullException(nameof(url));
        }

        try
        {
            // UseShellExecute is what makes a URL — rather than an executable path — a valid target.
            // There is nothing to wait for afterwards, so the handle the shell hands back is released
            // straight away.
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();

            return Task.FromResult(true);
        }
        catch (Exception exception) when (exception is Win32Exception
                                             or InvalidOperationException
                                             or ObjectDisposedException)
        {
            // No association for http(s), or the shell declined. Deliberately narrow: anything else is a
            // bug worth surfacing rather than swallowing.
            return Task.FromResult(false);
        }
    }
}
