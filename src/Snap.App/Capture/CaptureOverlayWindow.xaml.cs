using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Snap.App.Storage;
using Snap.Core.Capture;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;
using WpfEllipse = System.Windows.Shapes.Ellipse;
using WpfPath = System.Windows.Shapes.Path;
using WpfPolyline = System.Windows.Shapes.Polyline;
using WpfRectangle = System.Windows.Shapes.Rectangle;

namespace Snap.App.Capture;

public partial class CaptureOverlayWindow : Window
{
    private readonly CaptureVault _vault;
    private readonly CapturedDesktop _desktop;
    private readonly WindowSelectionService _windowSelectionService;
    private readonly UndoHistory<UIElement> _undoHistory = new();
    private WpfPoint _startPoint;
    private WpfPoint _annotationStart;
    private Rect _selection;
    private Rect _clickCandidate;
    private bool _isSelecting;
    private bool _isMovingSelection;
    private Rect _moveStartSelection;
    private AnnotationTool _activeTool;
    private FrameworkElement? _activeAnnotation;
    private Button? _activeToolButton;
    private Color _annotationColor = Color.FromRgb(250, 81, 81);
    private double _strokeThickness = 4;
    private TextSize _textSize = TextSize.Small;
    private TextBox? _selectedText;
    private ArrowAnnotationState? _selectedArrow;
    private bool _isMovingArrow;
    private WpfPoint _arrowMoveStart;
    private WpfPoint _arrowOriginalStart;
    private WpfPoint _arrowOriginalEnd;
    private System.Windows.Shapes.Shape? _selectedShape;
    private bool _isMovingShape;
    private WpfPoint _shapeMoveStart;
    private PixelRect _shapeOriginalBounds;

    public CaptureOverlayWindow(
        ScreenCaptureService captureService,
        WindowSelectionService windowSelectionService,
        CaptureVault vault)
    {
        ArgumentNullException.ThrowIfNull(captureService);
        _windowSelectionService = windowSelectionService ?? throw new ArgumentNullException(nameof(windowSelectionService));
        _vault = vault ?? throw new ArgumentNullException(nameof(vault));
        _desktop = captureService.Capture();

        InitializeComponent();
        DesktopImage.Source = _desktop.Image;
        Left = _desktop.VirtualBounds.Left;
        Top = _desktop.VirtualBounds.Top;
        Width = _desktop.VirtualBounds.Width;
        Height = _desktop.VirtualBounds.Height;
        Loaded += (_, _) =>
        {
            ResetSelection();
            Activate();
        };
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source &&
            (ActionBar.IsAncestorOf(source) || ToolOptionsBar.IsAncestorOf(source) ||
             ReferenceEquals(source, ActionBar) || ReferenceEquals(source, ToolOptionsBar))) return;
        if (e.ClickCount == 2 && !_selection.IsEmpty && _selection.Contains(e.GetPosition(OverlayCanvas)))
        {
            CompleteCapture();
            return;
        }

        if (FindParent<ButtonBase>(e.OriginalSource as DependencyObject) is not null ||
            FindParent<Thumb>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        _startPoint = e.GetPosition(OverlayCanvas);
        _clickCandidate = _selection;
        _selection = Rect.Empty;
        _isSelecting = true;
        ActionBar.Visibility = Visibility.Collapsed;
        ToolOptionsBar.Visibility = Visibility.Collapsed;
        AnnotationCanvas.Visibility = Visibility.Collapsed;
        SelectionHandles.Visibility = Visibility.Collapsed;
        DeselectShape();
        DeselectArrow();
        SelectionBorder.Visibility = Visibility.Visible;
        SizeBadge.Visibility = Visibility.Visible;
        OverlayCanvas.CaptureMouse();
        UpdateSelection(_startPoint);
    }

    private void OnMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (_isSelecting)
        {
            UpdateSelection(e.GetPosition(OverlayCanvas));
            return;
        }

        if (_activeTool is AnnotationTool.None && ActionBar.Visibility is Visibility.Collapsed)
        {
            UpdateWindowCandidate(e.GetPosition(OverlayCanvas));
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting)
        {
            return;
        }

        _isSelecting = false;
        OverlayCanvas.ReleaseMouseCapture();
        UpdateSelection(e.GetPosition(OverlayCanvas));
        if (_selection.Width < 2 || _selection.Height < 2)
        {
            if (!_clickCandidate.IsEmpty)
            {
                _selection = _clickCandidate;
                ApplySelectionBounds();
            }
            else
            {
                ResetSelection();
                return;
            }
        }

        ActionBar.Visibility = Visibility.Visible;
        PositionActionBar();
        AnnotationCanvas.Visibility = Visibility.Visible;
        SelectionHandles.Visibility = Visibility.Visible;
    }

    private void OnKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.OriginalSource is TextBox)
        {
            return;
        }

        if (e.Key is Key.Escape)
        {
            Close();
        }
        else if (e.Key is Key.Delete or Key.Back && _selectedShape is not null)
        {
            AnnotationCanvas.Children.Remove(_selectedShape);
            _undoHistory.Remove(_selectedShape);
            DeselectShape();
            UndoButton.IsEnabled = _undoHistory.Count > 0;
            e.Handled = true;
        }
        else if (e.Key is Key.Delete or Key.Back && _selectedArrow is not null)
        {
            DeleteSelectedArrow();
            e.Handled = true;
        }
        else if (e.Key is Key.Enter && !_selection.IsEmpty)
        {
            CompleteCapture();
        }
    }

    private void OnDoneClick(object sender, RoutedEventArgs e) => CompleteCapture();

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();

    private void OnPinClick(object sender, RoutedEventArgs e)
    {
        var image = CreateResultImage();
        if (image is null)
        {
            return;
        }

        _vault.Save(image);
        new PinnedImageWindow(image).Show();
        Close();
    }

    private void CompleteCapture()
    {
        var image = CreateResultImage();
        if (image is null)
        {
            return;
        }

        Clipboard.SetImage(image);
        _vault.Save(image);
        Close();
    }

    private BitmapSource? CreateResultImage()
    {
        if (_selection.IsEmpty || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return null;
        }

        var scaleX = _desktop.Image.PixelWidth / ActualWidth;
        var scaleY = _desktop.Image.PixelHeight / ActualHeight;
        var source = new Int32Rect(
            (int)Math.Floor(_selection.X * scaleX),
            (int)Math.Floor(_selection.Y * scaleY),
            Math.Max(1, (int)Math.Round(_selection.Width * scaleX)),
            Math.Max(1, (int)Math.Round(_selection.Height * scaleY)));
        source.Width = Math.Min(source.Width, _desktop.Image.PixelWidth - source.X);
        source.Height = Math.Min(source.Height, _desktop.Image.PixelHeight - source.Y);

        var crop = new CroppedBitmap(_desktop.Image, source);
        crop.Freeze();
        if (AnnotationCanvas.Children.Count == 0)
        {
            return crop;
        }

        AnnotationCanvas.Measure(new Size(_selection.Width, _selection.Height));
        AnnotationCanvas.Arrange(new Rect(0, 0, _selection.Width, _selection.Height));
        AnnotationCanvas.UpdateLayout();

        var annotations = new RenderTargetBitmap(
            source.Width,
            source.Height,
            96 * scaleX,
            96 * scaleY,
            PixelFormats.Pbgra32);
        annotations.Render(AnnotationCanvas);
        annotations.Freeze();

        var drawing = new DrawingVisual();
        using (var context = drawing.RenderOpen())
        {
            var bounds = new Rect(0, 0, source.Width, source.Height);
            context.DrawImage(crop, bounds);
            context.DrawImage(annotations, bounds);
        }

        var result = new RenderTargetBitmap(source.Width, source.Height, 96, 96, PixelFormats.Pbgra32);
        result.Render(drawing);
        result.Freeze();
        return result;
    }

    private void OnToolClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string toolName } && Enum.TryParse(toolName, out AnnotationTool tool))
        {
            if (_activeToolButton is not null)
            {
                _activeToolButton.Background = Brushes.Transparent;
            }

            _activeToolButton = (Button)sender;
            _activeToolButton.Background = new SolidColorBrush(Color.FromRgb(228, 241, 234));
            _activeTool = tool;
            DeselectShape();
            if (tool is not AnnotationTool.Arrow)
            {
                DeselectArrow();
            }
            if (tool is not AnnotationTool.Text)
            {
                _selectedText = null;
            }

            ShowSizeOptionsForTool(tool);
            AnnotationCanvas.Cursor = tool is AnnotationTool.Text ? Cursors.IBeam : Cursors.Cross;
            ToolOptionsBar.Visibility = tool is AnnotationTool.Rectangle or AnnotationTool.Ellipse or
                AnnotationTool.Arrow or AnnotationTool.Pen or AnnotationTool.Text
                ? Visibility.Visible
                : Visibility.Collapsed;
            PositionToolOptionsBar();
        }
    }

    private void OnColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string colorText } button ||
            ColorConverter.ConvertFromString(colorText) is not Color color)
        {
            return;
        }

        _annotationColor = color;
        foreach (var item in ColorPanel.Children.OfType<Button>())
        {
            item.Background = Brushes.Transparent;
            item.BorderBrush = Brushes.Transparent;
        }

        button.Background = new SolidColorBrush(Color.FromRgb(236, 236, 236));
        button.BorderBrush = new SolidColorBrush(Color.FromRgb(207, 207, 207));
        if (_selectedArrow is not null)
        {
            _selectedArrow.Path.Fill = new SolidColorBrush(color);
        }
        if (_selectedShape is not null) _selectedShape.Stroke = new SolidColorBrush(color);
    }

    private void OnStrokeSizeClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string sizeText } || !double.TryParse(sizeText, out var thickness))
        {
            return;
        }

        _strokeThickness = thickness;
        var inactive = new SolidColorBrush(Color.FromRgb(163, 163, 163));
        ThinSizeDot.Fill = inactive;
        MediumSizeDot.Fill = inactive;
        ThickSizeDot.Fill = inactive;
        var active = new SolidColorBrush(Color.FromRgb(7, 193, 96));
        if (thickness <= 2) ThinSizeDot.Fill = active;
        else if (thickness <= 4) MediumSizeDot.Fill = active;
        else ThickSizeDot.Fill = active;
        if (_selectedArrow is not null)
        {
            _selectedArrow.ShaftWidth = thickness;
            UpdateArrowVisual(_selectedArrow);
        }
        if (_selectedShape is not null) _selectedShape.StrokeThickness = thickness;
    }

    private void OnTextSizeClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string sizeText } || !Enum.TryParse(sizeText, out TextSize size))
        {
            return;
        }

        _textSize = size;
        var inactive = new SolidColorBrush(Color.FromRgb(163, 163, 163));
        foreach (var button in TextSizePanel.Children.OfType<Button>())
        {
            button.Foreground = inactive;
        }

        ((Button)sender).Foreground = new SolidColorBrush(Color.FromRgb(7, 193, 96));
        if (_selectedText is not null)
        {
            _selectedText.FontSize = TextSizing.ToFontSize(size);
        }
    }

    private void ShowSizeOptionsForTool(AnnotationTool tool)
    {
        var isText = tool is AnnotationTool.Text;
        StrokeSizePanel.Visibility = isText ? Visibility.Collapsed : Visibility.Visible;
        TextSizePanel.Visibility = isText ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnUndoClick(object sender, RoutedEventArgs e)
    {
        if (_undoHistory.TryPop(out var item) && item is not null)
        {
            AnnotationCanvas.Children.Remove(item);
            if (_selectedShape == item) DeselectShape();
            if (_selectedArrow?.Path == item)
            {
                DeselectArrow();
            }
        }

        UndoButton.IsEnabled = _undoHistory.Count > 0;
    }

    private void OnUnavailableToolClick(object sender, RoutedEventArgs e) =>
        MessageBox.Show(this, "滚动长截图将在后续版本开放。", "Snap", MessageBoxButton.OK, MessageBoxImage.Information);

    private void OnAnnotationMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<System.Windows.Shapes.Shape>(e.OriginalSource as DependencyObject) is { } shape &&
            shape is WpfRectangle or WpfEllipse && !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            SelectShape(shape);
            _isMovingShape = true;
            _shapeMoveStart = e.GetPosition(AnnotationCanvas);
            _shapeOriginalBounds = new PixelRect(Canvas.GetLeft(shape), Canvas.GetTop(shape), shape.Width, shape.Height);
            AnnotationCanvas.CaptureMouse();
            e.Handled = true;
            return;
        }
        if (FindParent<WpfPath>(e.OriginalSource as DependencyObject) is { Tag: ArrowAnnotationState arrow })
        {
            SelectArrow(arrow);
            _isMovingArrow = true;
            _arrowMoveStart = e.GetPosition(AnnotationCanvas);
            _arrowOriginalStart = arrow.Start;
            _arrowOriginalEnd = arrow.End;
            AnnotationCanvas.CaptureMouse();
            e.Handled = true;
            return;
        }

        DeselectShape();
        DeselectArrow();
        if (_activeTool is AnnotationTool.None)
        {
            if (e.ClickCount == 2)
            {
                CompleteCapture();
                e.Handled = true;
                return;
            }

            _isMovingSelection = true;
            _startPoint = e.GetPosition(OverlayCanvas);
            _moveStartSelection = _selection;
            ActionBar.Visibility = Visibility.Collapsed;
            AnnotationCanvas.CaptureMouse();
            e.Handled = true;
            return;
        }

        _annotationStart = e.GetPosition(AnnotationCanvas);
        _activeAnnotation = CreateAnnotation(_activeTool, _annotationStart);
        if (_activeAnnotation is null)
        {
            return;
        }

        AnnotationCanvas.Children.Add(_activeAnnotation);
        if (_activeAnnotation is TextBox textBox)
        {
            textBox.Focus();
            Keyboard.Focus(textBox);
        }

        if (_activeTool is AnnotationTool.Text or AnnotationTool.Emoji)
        {
            _undoHistory.Push(_activeAnnotation);
            UndoButton.IsEnabled = true;
            _activeAnnotation = null;
        }
        else
        {
            AnnotationCanvas.CaptureMouse();
        }

        e.Handled = true;
    }

    private void OnAnnotationMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (_isMovingShape && _selectedShape is not null && e.LeftButton is MouseButtonState.Pressed)
        {
            var current = e.GetPosition(AnnotationCanvas);
            var moved = AnnotationMovement.Move(_shapeOriginalBounds,
                current.X - _shapeMoveStart.X, current.Y - _shapeMoveStart.Y,
                new PixelRect(0, 0, AnnotationCanvas.ActualWidth, AnnotationCanvas.ActualHeight));
            Canvas.SetLeft(_selectedShape, moved.X);
            Canvas.SetTop(_selectedShape, moved.Y);
            PositionShapeSelection();
            e.Handled = true;
            return;
        }
        if (_isMovingArrow && _selectedArrow is not null && e.LeftButton is MouseButtonState.Pressed)
        {
            var current = e.GetPosition(AnnotationCanvas);
            var translated = ArrowGeometry.TranslateWithinBounds(
                new PixelPoint(_arrowOriginalStart.X, _arrowOriginalStart.Y),
                new PixelPoint(_arrowOriginalEnd.X, _arrowOriginalEnd.Y),
                current.X - _arrowMoveStart.X,
                current.Y - _arrowMoveStart.Y,
                new PixelRect(0, 0, AnnotationCanvas.ActualWidth, AnnotationCanvas.ActualHeight));
            _selectedArrow.Start = new WpfPoint(translated.Start.X, translated.Start.Y);
            _selectedArrow.End = new WpfPoint(translated.End.X, translated.End.Y);
            UpdateArrowVisual(_selectedArrow);
            e.Handled = true;
            return;
        }

        if (_isMovingSelection && e.LeftButton is MouseButtonState.Pressed)
        {
            var current = e.GetPosition(OverlayCanvas);
            var bounds = new PixelRect(0, 0, OverlayCanvas.ActualWidth, OverlayCanvas.ActualHeight);
            var moved = SelectionAdjustment.Move(
                ToPixelRect(_moveStartSelection),
                current.X - _startPoint.X,
                current.Y - _startPoint.Y,
                bounds);
            _selection = ToRect(moved);
            ApplySelectionBounds();
            e.Handled = true;
            return;
        }

        if (_activeAnnotation is null || e.LeftButton is not MouseButtonState.Pressed)
        {
            return;
        }

        UpdateAnnotation(_activeAnnotation, e.GetPosition(AnnotationCanvas));
        e.Handled = true;
    }

    private void OnAnnotationMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isMovingShape)
        {
            _isMovingShape = false;
            AnnotationCanvas.ReleaseMouseCapture();
            e.Handled = true;
            return;
        }
        if (_isMovingArrow)
        {
            _isMovingArrow = false;
            AnnotationCanvas.ReleaseMouseCapture();
            e.Handled = true;
            return;
        }

        if (_isMovingSelection)
        {
            _isMovingSelection = false;
            AnnotationCanvas.ReleaseMouseCapture();
            ActionBar.Visibility = Visibility.Visible;
            PositionActionBar();
            e.Handled = true;
            return;
        }

        if (_activeAnnotation is null)
        {
            return;
        }

        UpdateAnnotation(_activeAnnotation, e.GetPosition(AnnotationCanvas));
        AnnotationCanvas.ReleaseMouseCapture();
        _undoHistory.Push(_activeAnnotation);
        if (_activeAnnotation is WpfPath { Tag: ArrowAnnotationState arrow })
        {
            SelectArrow(arrow);
        }
        else if (_activeAnnotation is WpfRectangle or WpfEllipse)
        {
            SelectShape((System.Windows.Shapes.Shape)_activeAnnotation);
        }

        UndoButton.IsEnabled = true;
        _activeAnnotation = null;
        e.Handled = true;
    }

    private FrameworkElement? CreateAnnotation(AnnotationTool tool, WpfPoint point)
    {
        var stroke = new SolidColorBrush(_annotationColor);
        switch (tool)
        {
            case AnnotationTool.Rectangle:
                return PlaceAt(new WpfRectangle
                {
                    Stroke = stroke,
                    StrokeThickness = _strokeThickness,
                    Fill = Brushes.Transparent,
                    Cursor = Cursors.SizeAll,
                }, point);
            case AnnotationTool.Ellipse:
                return PlaceAt(new WpfEllipse
                {
                    Stroke = stroke,
                    StrokeThickness = _strokeThickness,
                    Fill = Brushes.Transparent,
                    Cursor = Cursors.SizeAll,
                }, point);
            case AnnotationTool.Arrow:
                return CreateArrowAnnotation(point, stroke);
            case AnnotationTool.Pen:
                var pen = new WpfPolyline
                {
                    Stroke = stroke,
                    StrokeThickness = _strokeThickness,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                };
                pen.Points.Add(point);
                return pen;
            case AnnotationTool.Mosaic:
                var mosaic = new MosaicStrokeElement(
                    CreatePixelatedSelection(),
                    _selection.Width,
                    _selection.Height);
                mosaic.AddPoint(point);
                return mosaic;
            case AnnotationTool.Text:
                var textBox = PlaceAt(new TextBox
                {
                    MinWidth = 80,
                    FontSize = TextSizing.ToFontSize(_textSize),
                    Foreground = stroke,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    AcceptsReturn = true,
                }, point);
                textBox.GotKeyboardFocus += OnTextBoxGotKeyboardFocus;
                _selectedText = textBox;
                return textBox;
            case AnnotationTool.Emoji:
                return PlaceAt(new TextBlock
                {
                    Text = "🙂",
                    FontFamily = new FontFamily("Segoe UI Emoji"),
                    FontSize = 36,
                }, point);
            default:
                return null;
        }
    }

    private static T PlaceAt<T>(T element, WpfPoint point) where T : FrameworkElement
    {
        Canvas.SetLeft(element, point.X);
        Canvas.SetTop(element, point.Y);
        return element;
    }

    private void UpdateAnnotation(FrameworkElement element, WpfPoint current)
    {
        if (element is WpfPath { Tag: ArrowAnnotationState arrow })
        {
            arrow.End = current;
            UpdateArrowVisual(arrow);
            return;
        }

        if (element is MosaicStrokeElement mosaic)
        {
            mosaic.AddPoint(current);
            return;
        }

        if (element is WpfPolyline polyline)
        {
            polyline.Points.Add(current);
            return;
        }

        var rectangle = SelectionGeometry.FromPoints(
            new PixelPoint(_annotationStart.X, _annotationStart.Y),
            new PixelPoint(current.X, current.Y));
        Canvas.SetLeft(element, rectangle.X);
        Canvas.SetTop(element, rectangle.Y);
        element.Width = rectangle.Width;
        element.Height = rectangle.Height;
    }

    private WpfPath CreateArrowAnnotation(WpfPoint point, Brush fill)
    {
        var path = new WpfPath
        {
            Fill = fill,
            Cursor = Cursors.SizeAll
        };
        var state = new ArrowAnnotationState(path, point, point, _strokeThickness);
        path.Tag = state;
        UpdateArrowVisual(state);
        return path;
    }

    private static Geometry CreateArrowGeometry(WpfPoint start, WpfPoint end, double shaftWidth)
    {
        var dimensions = ArrowGeometry.DimensionsForSize(shaftWidth);
        var polygon = ArrowGeometry.CalculateFilledArrow(
            new PixelPoint(start.X, start.Y),
            new PixelPoint(end.X, end.Y),
            dimensions.ShaftWidth,
            dimensions.HeadLength,
            dimensions.HeadWidth);
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            var first = polygon.Points[0];
            context.BeginFigure(new WpfPoint(first.X, first.Y), isFilled: true, isClosed: true);
            foreach (var point in polygon.Points.Skip(1))
            {
                context.LineTo(new WpfPoint(point.X, point.Y), isStroked: true, isSmoothJoin: true);
            }
        }

        return geometry;
    }

    private void UpdateArrowVisual(ArrowAnnotationState arrow)
    {
        arrow.Path.Data = CreateArrowGeometry(arrow.Start, arrow.End, arrow.ShaftWidth);
        if (_selectedArrow == arrow)
        {
            PositionArrowHandles();
        }
    }

    private BitmapSource CreatePixelatedSelection()
    {
        var crop = CreateDesktopCrop();
        const double blockSize = 14;
        var pixelWidth = Math.Max(1, (int)Math.Ceiling(crop.PixelWidth / blockSize));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(crop.PixelHeight / blockSize));
        var pixelated = new TransformedBitmap(
            crop,
            new ScaleTransform(
                pixelWidth / (double)crop.PixelWidth,
                pixelHeight / (double)crop.PixelHeight));
        pixelated.Freeze();
        return pixelated;
    }

    private BitmapSource CreateDesktopCrop()
    {
        var scaleX = _desktop.Image.PixelWidth / ActualWidth;
        var scaleY = _desktop.Image.PixelHeight / ActualHeight;
        var source = new Int32Rect(
            (int)Math.Floor(_selection.X * scaleX),
            (int)Math.Floor(_selection.Y * scaleY),
            Math.Max(1, (int)Math.Round(_selection.Width * scaleX)),
            Math.Max(1, (int)Math.Round(_selection.Height * scaleY)));
        source.Width = Math.Min(source.Width, _desktop.Image.PixelWidth - source.X);
        source.Height = Math.Min(source.Height, _desktop.Image.PixelHeight - source.Y);
        var crop = new CroppedBitmap(_desktop.Image, source);
        crop.Freeze();
        return crop;
    }

    private void UpdateSelection(WpfPoint currentPoint)
    {
        var normalized = SelectionGeometry.FromPoints(
            new PixelPoint(_startPoint.X, _startPoint.Y),
            new PixelPoint(currentPoint.X, currentPoint.Y));
        var clamped = SelectionGeometry.Clamp(
            normalized,
            new PixelRect(0, 0, OverlayCanvas.ActualWidth, OverlayCanvas.ActualHeight));
        _selection = new Rect(clamped.X, clamped.Y, clamped.Width, clamped.Height);

        ApplySelectionBounds();
    }

    private void ApplySelectionBounds()
    {
        SetBounds(SelectionBorder, _selection.X, _selection.Y, _selection.Width, _selection.Height);
        SetBounds(AnnotationCanvas, _selection.X, _selection.Y, _selection.Width, _selection.Height);
        SetBounds(SelectionHandles, _selection.X, _selection.Y, _selection.Width, _selection.Height);
        SetBounds(ArrowEditCanvas, _selection.X, _selection.Y, _selection.Width, _selection.Height);
        PositionSelectionHandles();
        PositionArrowHandles();
        PositionShapeSelection();
        AnnotationCanvas.Visibility = ActionBar.Visibility is Visibility.Visible || _isMovingSelection
            ? Visibility.Visible
            : Visibility.Collapsed;
        SizeText.Text = $"{Math.Round(_selection.Width):0} × {Math.Round(_selection.Height):0}";
        SizeBadge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(SizeBadge, _selection.X);
        Canvas.SetTop(SizeBadge, Math.Max(0, _selection.Y - SizeBadge.DesiredSize.Height - 5));
        UpdateShade();
    }

    private void UpdateWindowCandidate(WpfPoint localPoint)
    {
        var screenPoint = new PixelPoint(
            localPoint.X + _desktop.VirtualBounds.Left,
            localPoint.Y + _desktop.VirtualBounds.Top);
        var window = _windowSelectionService.FindTopLevelWindowAt(screenPoint, (uint)Environment.ProcessId);
        if (window is null)
        {
            return;
        }

        var local = new PixelRect(
            window.Value.X - _desktop.VirtualBounds.Left,
            window.Value.Y - _desktop.VirtualBounds.Top,
            window.Value.Width,
            window.Value.Height);
        var clamped = SelectionGeometry.Clamp(
            local,
            new PixelRect(0, 0, OverlayCanvas.ActualWidth, OverlayCanvas.ActualHeight));
        _selection = new Rect(clamped.X, clamped.Y, clamped.Width, clamped.Height);
        SelectionBorder.Visibility = Visibility.Visible;
        SelectionHandles.Visibility = Visibility.Collapsed;
        SizeBadge.Visibility = Visibility.Visible;
        ApplySelectionBounds();
    }

    private void UpdateShade()
    {
        SetBounds(TopShade, 0, 0, OverlayCanvas.ActualWidth, _selection.Y);
        SetBounds(LeftShade, 0, _selection.Y, _selection.X, _selection.Height);
        SetBounds(
            RightShade,
            _selection.Right,
            _selection.Y,
            Math.Max(0, OverlayCanvas.ActualWidth - _selection.Right),
            _selection.Height);
        SetBounds(
            BottomShade,
            0,
            _selection.Bottom,
            OverlayCanvas.ActualWidth,
            Math.Max(0, OverlayCanvas.ActualHeight - _selection.Bottom));
    }

    private void PositionActionBar()
    {
        if (_selection.IsEmpty || ActionBar.Visibility != Visibility.Visible) return;
        var screen = GetToolbarScreenBounds();
        var optionsVisible = ToolOptionsBar.Visibility == Visibility.Visible;
        ActionBar.LayoutTransform = Transform.Identity;
        ToolOptionsBar.LayoutTransform = Transform.Identity;
        ActionBar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        ToolOptionsBar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = Math.Max(ActionBar.DesiredSize.Width, optionsVisible ? ToolOptionsBar.DesiredSize.Width : 0);
        var height = ActionBar.DesiredSize.Height + (optionsVisible ? ToolOptionsBar.DesiredSize.Height + 5 : 0);
        var scale = Math.Min(1, Math.Min(screen.Width / Math.Max(1, width), screen.Height / Math.Max(1, height)));
        ActionBar.LayoutTransform = new ScaleTransform(scale, scale);
        ToolOptionsBar.LayoutTransform = new ScaleTransform(scale, scale);
        ActionBar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        ToolOptionsBar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        width = Math.Max(ActionBar.DesiredSize.Width, optionsVisible ? ToolOptionsBar.DesiredSize.Width : 0);
        height = ActionBar.DesiredSize.Height + (optionsVisible ? ToolOptionsBar.DesiredSize.Height + 5 : 0);
        var placement = ToolbarPlacement.Place(ToPixelRect(_selection), width, height, ToPixelRect(screen));
        Canvas.SetLeft(ActionBar, placement.X);
        Canvas.SetTop(ActionBar, placement.Y);
        Canvas.SetLeft(ToolOptionsBar, placement.X);
        Canvas.SetTop(ToolOptionsBar, placement.Y + ActionBar.DesiredSize.Height + 5);
    }

    private void PositionToolOptionsBar() => PositionActionBar();

    private Rect GetToolbarScreenBounds()
    {
        var anchor = OverlayCanvas.PointToScreen(new WpfPoint(_selection.Right - 1, _selection.Bottom - 1));
        var area = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)anchor.X, (int)anchor.Y)).WorkingArea;
        var topLeft = OverlayCanvas.PointFromScreen(new WpfPoint(area.Left, area.Top));
        var bottomRight = OverlayCanvas.PointFromScreen(new WpfPoint(area.Right, area.Bottom));
        var bounds = Rect.Intersect(new Rect(topLeft, bottomRight), new Rect(0, 0, OverlayCanvas.ActualWidth, OverlayCanvas.ActualHeight));
        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
            bounds = new Rect(0, 0, OverlayCanvas.ActualWidth, OverlayCanvas.ActualHeight);
        var margin = Math.Min(6, Math.Min(bounds.Width, bounds.Height) / 4);
        bounds.Inflate(-margin, -margin);
        return bounds;
    }

    private void ResetSelection()
    {
        _selection = Rect.Empty;
        SelectionBorder.Visibility = Visibility.Collapsed;
        SelectionHandles.Visibility = Visibility.Collapsed;
        AnnotationCanvas.Visibility = Visibility.Collapsed;
        AnnotationCanvas.Children.Clear();
        DeselectShape();
        DeselectArrow();
        SizeBadge.Visibility = Visibility.Collapsed;
        ActionBar.Visibility = Visibility.Collapsed;
        ToolOptionsBar.Visibility = Visibility.Collapsed;
        SetBounds(TopShade, 0, 0, OverlayCanvas.ActualWidth, OverlayCanvas.ActualHeight);
        SetBounds(LeftShade, 0, 0, 0, 0);
        SetBounds(RightShade, 0, 0, 0, 0);
        SetBounds(BottomShade, 0, 0, 0, 0);
    }

    private static void SetBounds(FrameworkElement element, double left, double top, double width, double height)
    {
        Canvas.SetLeft(element, left);
        Canvas.SetTop(element, top);
        element.Width = Math.Max(0, width);
        element.Height = Math.Max(0, height);
    }

    private void OnSelectionHandleDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb { Tag: string handleName } ||
            !Enum.TryParse(handleName, out SelectionHandle handle))
        {
            return;
        }

        var resized = SelectionAdjustment.Resize(
            ToPixelRect(_selection),
            handle,
            e.HorizontalChange,
            e.VerticalChange,
            new PixelRect(0, 0, OverlayCanvas.ActualWidth, OverlayCanvas.ActualHeight),
            minimumSize: 8);
        _selection = ToRect(resized);
        ApplySelectionBounds();
        PositionActionBar();
        ActionBar.Visibility = Visibility.Visible;
    }

    private void OnArrowHandleDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_selectedArrow is null || sender is not Thumb { Tag: string endpoint })
        {
            return;
        }

        var current = endpoint == "Start" ? _selectedArrow.Start : _selectedArrow.End;
        var updated = new WpfPoint(
            Math.Clamp(current.X + e.HorizontalChange, 0, AnnotationCanvas.ActualWidth),
            Math.Clamp(current.Y + e.VerticalChange, 0, AnnotationCanvas.ActualHeight));
        if (endpoint == "Start")
        {
            _selectedArrow.Start = updated;
        }
        else
        {
            _selectedArrow.End = updated;
        }

        UpdateArrowVisual(_selectedArrow);
    }

    private void SelectArrow(ArrowAnnotationState arrow)
    {
        DeselectShape();
        _selectedText = null;
        _selectedArrow = arrow;
        ArrowEditCanvas.Visibility = Visibility.Visible;
        ToolOptionsBar.Visibility = Visibility.Visible;
        ShowSizeOptionsForTool(AnnotationTool.Arrow);
        PositionArrowHandles();
        PositionToolOptionsBar();
    }

    private void OnTextBoxGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            _selectedText = textBox;
        }
    }

    private void DeleteSelectedArrow()
    {
        if (_selectedArrow is null)
        {
            return;
        }

        var path = _selectedArrow.Path;
        AnnotationCanvas.Children.Remove(path);
        _undoHistory.Remove(path);
        DeselectArrow();
        UndoButton.IsEnabled = _undoHistory.Count > 0;
    }

    private void DeselectArrow()
    {
        _isMovingArrow = false;
        _selectedArrow = null;
        ArrowEditCanvas.Visibility = Visibility.Collapsed;
    }

    private void SelectShape(System.Windows.Shapes.Shape shape)
    {
        DeselectArrow();
        _selectedText = null;
        _selectedShape = shape;
        ShapeSelectionBorder.Visibility = Visibility.Visible;
        ToolOptionsBar.Visibility = Visibility.Visible;
        ShowSizeOptionsForTool(shape is WpfRectangle ? AnnotationTool.Rectangle : AnnotationTool.Ellipse);
        PositionShapeSelection();
        PositionActionBar();
    }

    private void DeselectShape()
    {
        _selectedShape = null;
        _isMovingShape = false;
        ShapeSelectionBorder.Visibility = Visibility.Collapsed;
    }

    private void PositionShapeSelection()
    {
        if (_selectedShape is null) return;
        SetBounds(ShapeSelectionBorder, _selection.X + Canvas.GetLeft(_selectedShape) - 3,
            _selection.Y + Canvas.GetTop(_selectedShape) - 3, _selectedShape.Width + 6, _selectedShape.Height + 6);
    }

    private void PositionArrowHandles()
    {
        if (_selectedArrow is null)
        {
            return;
        }

        const double half = 6;
        Canvas.SetLeft(ArrowStartHandle, _selectedArrow.Start.X - half);
        Canvas.SetTop(ArrowStartHandle, _selectedArrow.Start.Y - half);
        Canvas.SetLeft(ArrowEndHandle, _selectedArrow.End.X - half);
        Canvas.SetTop(ArrowEndHandle, _selectedArrow.End.Y - half);
    }

    private void PositionSelectionHandles()
    {
        const double half = 5;
        var positions = new[]
        {
            new WpfPoint(-half, -half),
            new WpfPoint(_selection.Width / 2 - half, -half),
            new WpfPoint(_selection.Width - half, -half),
            new WpfPoint(_selection.Width - half, _selection.Height / 2 - half),
            new WpfPoint(_selection.Width - half, _selection.Height - half),
            new WpfPoint(_selection.Width / 2 - half, _selection.Height - half),
            new WpfPoint(-half, _selection.Height - half),
            new WpfPoint(-half, _selection.Height / 2 - half),
        };

        for (var index = 0; index < SelectionHandles.Children.Count && index < positions.Length; index++)
        {
            if (SelectionHandles.Children[index] is FrameworkElement handle)
            {
                Canvas.SetLeft(handle, positions[index].X);
                Canvas.SetTop(handle, positions[index].Y);
            }
        }
    }

    private static PixelRect ToPixelRect(Rect value) => new(value.X, value.Y, value.Width, value.Height);

    private static Rect ToRect(PixelRect value) => new(value.X, value.Y, value.Width, value.Height);

    private static T? FindParent<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T match)
            {
                return match;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private sealed class ArrowAnnotationState(
        WpfPath path,
        WpfPoint start,
        WpfPoint end,
        double shaftWidth)
    {
        public WpfPath Path { get; } = path;

        public WpfPoint Start { get; set; } = start;

        public WpfPoint End { get; set; } = end;

        public double ShaftWidth { get; set; } = shaftWidth;
    }
}
