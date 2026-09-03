using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Controls;
using Enigma.Msi.Desktop.ViewModels;

namespace Enigma.Msi.Desktop.Views;

/// <summary>
/// The application window. Hosts the three control-library overlays (<c>HostDialog</c>,
/// <c>HostOverlay</c>, <c>HostInfoBar</c>), which <see cref="App"/> hands to their services at startup.
/// </summary>
public partial class MainWindow : Window
{
    private ObservableCollection<string>? _buildLog;

    /// <summary>Creates the window.</summary>
    public MainWindow() => InitializeComponent();

    /// <summary>
    /// Offers the quick start the first time the window appears on an empty form. The window's own
    /// lifecycle event is the only honest trigger for it: a ViewModel has no notion of being shown, and
    /// the command it delegates to carries the once-only and not-over-work guards itself.
    /// </summary>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ShowQuickStartOnStartupCommand.Execute(null);
        }
    }

    /// <summary>
    /// Keeps the log pane pinned to the newest line. Scrolling is a view concern, so it is wired here
    /// rather than pushed into the ViewModel.
    /// </summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_buildLog is not null)
        {
            _buildLog.CollectionChanged -= OnBuildLogChanged;
        }

        _buildLog = (DataContext as MainWindowViewModel)?.BuildLog;

        if (_buildLog is not null)
        {
            _buildLog.CollectionChanged += OnBuildLogChanged;
        }
    }

    private void OnBuildLogChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            LogScroller.ScrollToEnd();
        }
    }
}
