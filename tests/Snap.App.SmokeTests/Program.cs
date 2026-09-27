using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Snap.App.Capture;
using Snap.App.Storage;

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
    }
}
