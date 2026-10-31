using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Snap.App.Capture;
using Snap.App.Storage;
using Snap.App;
using Snap.Core.Settings;
using Snap.Core.Hotkeys;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Path = System.IO.Path;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        var window = new CaptureOverlayWindow(new ScreenCaptureService(), new WindowSelectionService(), new CaptureVault(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Snap-Verification")));
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object Field(string name) => typeof(CaptureOverlayWindow).GetField(name, flags)!.GetValue(window)!;
        var canvas = (Canvas)Field("AnnotationCanvas");
        var create = typeof(CaptureOverlayWindow).GetMethod("CreateAnnotation", flags)!;
        foreach (var tool in new[] { AnnotationTool.Rectangle, AnnotationTool.Ellipse })
        {
            var shape = (Shape)create.Invoke(window, new object[] { tool, new Point(20, 30) })!;
            shape.Width = 80; shape.Height = 60;
            canvas.Children.Add(shape);
            shape.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.MouseDownEvent });
            if (!ReferenceEquals(Field("_selectedShape"), shape)) throw new Exception(tool + " was not selected through mouse event routing");
            if (!(bool)Field("_isMovingShape")) throw new Exception(tool + " did not begin drag");
            shape.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.MouseUpEvent });
            if ((bool)Field("_isMovingShape")) throw new Exception("Drag did not end");
            var border = (Rectangle)Field("ShapeSelectionBorder");
            if (border.IsHitTestVisible || canvas.Children.Contains(border)) throw new Exception("Selection outline interferes with hit testing or export");
            canvas.Children.Remove(shape);
        }
        var bar = (FrameworkElement)Field("ActionBar");
        bar.Visibility = Visibility.Visible;
        bar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        if (bar.DesiredSize.Width < 500 || bar.DesiredSize.Height < 40) throw new Exception("Visible toolbar measured incorrectly");
        window.Close();
        Console.WriteLine("WPF event routing passed for rectangle and ellipse; drag state, export outline isolation and visible toolbar measurement passed.");
        VerifyVaultAndPreview();
    }

    static void VerifyVaultAndPreview()
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var folder = Path.Combine(Path.GetTempPath(), "Snap-UI-Verification");
        Directory.CreateDirectory(folder);
        var settings = AppSettings.CreateDefault() with { CaptureHotkey = new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x53) };
        var retentionWrites = 0;
        var main = new MainWindow(new CaptureVault(folder), () => { }, settings,
            _ => Task.FromResult(true), _ => { retentionWrites++; return Task.CompletedTask; });
        object Field(string name) => typeof(MainWindow).GetField(name, flags)!.GetValue(main)!;
        var hotkeyBox = (TextBox)Field("CaptureHotkeyBox");
        main.OpenSettings();
        if (hotkeyBox.Text != "Ctrl + Shift + S") throw new Exception("Opening settings hid the saved hotkey");
        main.Show();
        main.UpdateLayout();
        hotkeyBox.Focus();
        if (hotkeyBox.Text != "Ctrl + Shift + S") throw new Exception("Focus hid the saved hotkey");
        main.UpdateSettings(settings with { RetentionPeriod = Snap.Core.Storage.RetentionPeriod.ThirtyDays });
        if (retentionWrites != 0) throw new Exception("Displaying settings unexpectedly saved them again");
        typeof(MainWindow).GetMethod("OnChangeHotkeyClick", flags)!.Invoke(main, new object[] { main, new RoutedEventArgs() });
        if (!(bool)Field("_isRecordingHotkey")) throw new Exception("Change button failed to start recording");
        hotkeyBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(main), 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        if ((bool)Field("_isRecordingHotkey") || hotkeyBox.Text != "Ctrl + Shift + S") throw new Exception("Escape did not restore the saved hotkey");
        SaveRender(main, Path.Combine(folder, "settings.png"));

        var drawing = new DrawingVisual();
        using (var dc = drawing.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(239, 242, 234)), null, new Rect(0, 0, 2400, 1600));
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(49, 92, 67)), null, new Rect(120, 120, 2160, 160));
            for (var i = 0; i < 5; i++)
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb((byte)(90 + i * 20), (byte)(140 + i * 10), 115)), null, new Rect(200 + i * 400, 1200 - i * 160, 240, 200 + i * 160));
        }
        var image = new RenderTargetBitmap(2400, 1600, 96, 96, PixelFormats.Pbgra32);
        image.Render(drawing);
        image.Freeze();
        var imagePath = Path.Combine(folder, "fixture.png");
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using (var file = File.Create(imagePath)) encoder.Save(file);
        for (var i = 0; i < 6; i++) main.Captures.Add(CapturePreview.Load(imagePath, i == 0));
        ((TabControl)Field("MainTabs")).SelectedIndex = 0;
        main.UpdateLayout();
        SaveRender(main, Path.Combine(folder, "vault.png"));
        typeof(MainWindow).GetMethod("OnOpenCaptureClick", flags)!.Invoke(main,
            new object[] { new Button { DataContext = main.Captures[0] }, new RoutedEventArgs() });
        var preview = main.OwnedWindows.OfType<CaptureImageWindow>().Single();
        preview.UpdateLayout();
        var actions = preview.ContextMenu.Items.OfType<MenuItem>().ToArray();
        if (!actions.Select(item => item.Header.ToString()).SequenceEqual(new[] { "复制", "另存为…", "钉到桌面", "保留 / 取消保留", "删除" }))
            throw new Exception("Preview context menu actions are missing or out of order");
        var vault = new CaptureVault(folder);
        var originalKept = vault.IsKept(imagePath);
        actions[3].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        if (vault.IsKept(imagePath) == originalKept) throw new Exception("Preview keep action did not update the vault");
        actions[3].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        if (vault.IsKept(imagePath) != originalKept) throw new Exception("Repeated preview keep action used stale state");
        var previewImage = (Image)typeof(CaptureImageWindow).GetField("PreviewImage", flags)!.GetValue(preview)!;
        if (((BitmapSource)previewImage.Source).PixelWidth != 2400) throw new Exception("Preview opened a thumbnail instead of the original");
        var fitWidth = previewImage.Width;
        var scroll = (ScrollViewer)typeof(CaptureImageWindow).GetField("ImageScroll", flags)!.GetValue(preview)!;
        if (fitWidth <= 0 || fitWidth > scroll.ViewportWidth || previewImage.Height > scroll.ViewportHeight) throw new Exception("Image did not open fitted to the window");
        var pin = (System.Windows.Controls.Primitives.ToggleButton)typeof(CaptureImageWindow).GetField("PinPreviewButton", flags)!.GetValue(preview)!;
        if (preview.Topmost) throw new Exception("Preview unexpectedly opened pinned");
        pin.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, true);
        if (!preview.Topmost) throw new Exception("Pin button did not keep the preview on top");
        pin.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, false);
        if (preview.Topmost) throw new Exception("Pin button did not release the preview");
        SaveRender(preview, Path.Combine(folder, "preview.png"));
        previewImage.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, 120) { RoutedEvent = Mouse.PreviewMouseWheelEvent });
        if (previewImage.Width <= fitWidth) throw new Exception("Zoom-in did not enlarge the original");
        previewImage.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120) { RoutedEvent = Mouse.PreviewMouseWheelEvent });
        if (Math.Abs(previewImage.Width - fitWidth) > 1) throw new Exception("Wheel zoom-out did not restore the image size");
        var close = (Button)typeof(CaptureImageWindow).GetField("ClosePreviewButton", flags)!.GetValue(preview)!;
        close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (preview.IsVisible) throw new Exception("Close button did not close the preview");
        var small = new CaptureImageWindow(new CroppedBitmap(image, new Int32Rect(0, 0, 120, 80)), "10-31 12:00:00");
        small.Show(); small.UpdateLayout();
        var smallImage = (Image)typeof(CaptureImageWindow).GetField("PreviewImage", flags)!.GetValue(small)!;
        if (smallImage.Width <= 120) throw new Exception("Small image did not enlarge to fit the window");
        small.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(small), 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        if (small.IsVisible) throw new Exception("Escape did not close the preview");
        typeof(MainWindow).GetMethod("OnOpenCaptureClick", flags)!.Invoke(main,
            new object[] { new Button { DataContext = CapturePreview.Load(imagePath, vault.IsKept(imagePath)) }, new RoutedEventArgs() });
        var deletedPreview = main.OwnedWindows.OfType<CaptureImageWindow>().Single();
        deletedPreview.ContextMenu.Items.OfType<MenuItem>().Last().RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        if (File.Exists(imagePath) || deletedPreview.IsVisible || main.Captures.Any(capture => capture.FilePath == imagePath))
            throw new Exception("Preview delete did not remove the capture, close the preview and refresh the vault");
        main.Hide();
        Console.WriteLine("Saved-hotkey display, explicit recording/cancel, settings refresh, original-image opening, default fit, pin/unpin, wheel zoom, close/Escape and context-menu keep/delete passed.");
        Console.WriteLine("UI renders: " + folder);
    }

    static void SaveRender(Window window, string path)
    {
        window.UpdateLayout();
        var root = (FrameworkElement)VisualTreeHelper.GetChild(window, 0);
        var bitmap = new RenderTargetBitmap((int)root.ActualWidth, (int)root.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path); encoder.Save(file);
    }
}
