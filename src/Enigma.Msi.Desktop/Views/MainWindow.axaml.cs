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
