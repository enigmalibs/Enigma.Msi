using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace Enigma.Msi.Desktop.Views;

/// <summary>
/// The window shown while the app starts: the identity, for about two seconds, or less if the user is
/// in a hurry.
/// </summary>
/// <remarks>
/// The window deliberately does <em>not</em> close itself. The desktop lifetime's default
/// <c>ShutdownMode</c> is <c>OnLastWindowClose</c> — a last-window-standing rule — so closing the splash
/// before the main window is on screen would exit the process. <see cref="App"/> owns the handover and
/// listens for <see cref="Dismissed"/>.
/// </remarks>
public partial class SplashWindow : Window
{
    /// <summary>How long the splash stays up when nothing interrupts it.</summary>
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(2);

    private DispatcherTimer? _timer;
    private bool _isDismissed;

    /// <summary>Creates the splash.</summary>
    public SplashWindow() => InitializeComponent();

    /// <summary>
    /// Raised exactly once, when the splash's time is up or the user cut it short.
    /// </summary>
    public event EventHandler? Dismissed;

    /// <inheritdoc />
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        _timer = new DispatcherTimer { Interval = Duration };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        Dismiss();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        Dismiss();
    }

    private void OnTimerTick(object? sender, EventArgs e) => Dismiss();

    // Three ways in, one way out, and it has to fire once: the timer can come due while a click is
    // already being handled, and a key press arrives as both a down and a text event.
    private void Dismiss()
    {
        if (_isDismissed)
        {
            return;
        }

        _isDismissed = true;

        if (_timer is { } timer)
        {
            timer.Stop();
            timer.Tick -= OnTimerTick;
            _timer = null;
        }

        Dismissed?.Invoke(this, EventArgs.Empty);
    }
}
