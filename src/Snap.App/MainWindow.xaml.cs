using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Snap.App.Storage;

namespace Snap.App;

public partial class MainWindow : Window
{
    private readonly CaptureVault _vault;
    private readonly Action _beginCapture;

    public MainWindow(CaptureVault vault, Action beginCapture)
    {
        _vault = vault ?? throw new ArgumentNullException(nameof(vault));
        _beginCapture = beginCapture ?? throw new ArgumentNullException(nameof(beginCapture));
        InitializeComponent();
        DataContext = this;
    }

    public ObservableCollection<CapturePreview> Captures { get; } = [];

    public void RefreshCaptures()
    {
        Captures.Clear();
        foreach (var filePath in _vault.GetCaptures())
        {
            Captures.Add(CapturePreview.Load(filePath));
        }
    }

    private void OnStartCaptureClick(object sender, RoutedEventArgs e)
    {
        Hide();
        _beginCapture();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
