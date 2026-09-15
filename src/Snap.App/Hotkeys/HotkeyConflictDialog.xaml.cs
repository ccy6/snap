using System.Windows;
using Snap.Core.Hotkeys;

namespace Snap.App.Hotkeys;

public enum HotkeyConflictChoice
{
    CloseSnap,
    ChangeHotkey,
    Dormant,
}

public partial class HotkeyConflictDialog : Window
{
    public HotkeyConflictDialog(HotkeyGesture hotkey)
    {
        InitializeComponent();
        HotkeyText.Text = $"{hotkey.ToDisplayString()} 无法注册";
    }

    public HotkeyConflictChoice Choice { get; private set; } = HotkeyConflictChoice.ChangeHotkey;

    private void OnCloseSnapClick(object sender, RoutedEventArgs e) => Complete(HotkeyConflictChoice.CloseSnap);

    private void OnChangeClick(object sender, RoutedEventArgs e) => Complete(HotkeyConflictChoice.ChangeHotkey);

    private void OnDormantClick(object sender, RoutedEventArgs e) => Complete(HotkeyConflictChoice.Dormant);

    private void Complete(HotkeyConflictChoice choice)
    {
        Choice = choice;
        DialogResult = true;
    }
}
