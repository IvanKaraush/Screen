using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Screen.Services;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Color = System.Drawing.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace Screen;

public partial class MainWindow
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 9000;

    private NotifyIcon _trayIcon;
    private readonly ScreenCaptureService _screenCaptureService;
    private bool _printScreenPressed;

    private readonly ClipService _clipService;
    private readonly DrawArrow _drawArrow;
    private bool _isDrawingArrow;
    private static readonly Color DrawingColor = Color.Black;
    private readonly Brush _selectedColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(DrawingColor.A, DrawingColor.R, DrawingColor.G, DrawingColor.B));    public MainWindow()
    
    {
        _trayIcon = new NotifyIcon();
        _screenCaptureService = new ScreenCaptureService();
        InitializeComponent();
        InitializeTrayIcon();
        _clipService = new ClipService(ImageCanvas);
        _drawArrow = new DrawArrow(ImageCanvas, _selectedColor);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        RegisterHotKey(helper.Handle, HotkeyId, 0, (uint)Keys.PrintScreen);
        ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;
    }

    protected override void OnClosed(EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        UnregisterHotKey(helper.Handle, HotkeyId);
        ComponentDispatcher.ThreadPreprocessMessage -= ComponentDispatcher_ThreadPreprocessMessage;

        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = new Icon(SystemIcons.Application, 40, 40),
            Visible = true,
            Text = "Screen"
        };

        _trayIcon.ContextMenuStrip = new ContextMenuStrip();
        _trayIcon.ContextMenuStrip.Items.Add("Показать", null, ShowWindow);
        _trayIcon.ContextMenuStrip.Items.Add("Выход", null, ExitApplication);

        _trayIcon.DoubleClick += ShowWindow;
    }

    private void ComponentDispatcher_ThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
        if (msg.message == WmHotkey && (int)msg.wParam == HotkeyId && !_printScreenPressed)
        {
            _printScreenPressed = true;
            foreach (var screen in _screenCaptureService.Screens)
            {
                var overlay = _screenCaptureService.StartScreenCapture(screen);
                overlay.MouseLeftButtonUp += Overlay_MouseLeftButtonUp;
                overlay.MouseLeftButtonDown += Overlay_MouseLeftButtonDown;
                overlay.MouseMove += Overlay_MouseMove;

                overlay.KeyUp += HandleEscapeKey;
            }

            handled = true;
        }
    }

    private void HandleEscapeKey(object sender, KeyEventArgs args)
    {
        if (args.Key == Key.Escape)
        {
            foreach (var overlay in _screenCaptureService.Overlays)
            {
                overlay.Close();
            }

            _screenCaptureService.Overlays.Clear();
        }
    }

    private void Overlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ShowPreviewWindow(_screenCaptureService.StopCapture());
    }

    private void ShowPreviewWindow(Bitmap screenshot)
    {
        PreviewImage.Source = _screenCaptureService.BitmapToImageSource(screenshot);
        Show();
        _printScreenPressed = false;
    }

    private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var overlay = (Window)sender;
        _screenCaptureService.StartCapture(overlay, e.GetPosition(overlay));
    }

    private void ShowWindow(object? sender, EventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Current.Shutdown();
    }

    private void Overlay_MouseMove(object sender, MouseEventArgs e)
    {
        var overlay = (Window)sender;
        _screenCaptureService.ChangeScreenFigure(overlay, e.GetPosition(overlay));
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }

    private void AddRectangleButton_Click(object sender, RoutedEventArgs e)
    {
        var startPosition = new Point(50, 50);
        _clipService.AddRectangle(startPosition);
    }

    private void DrawArrowButton_Click(object sender, RoutedEventArgs e)
    {
        _isDrawingArrow = true;
    }

    private void ImageCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isDrawingArrow)
        {
            Point startPoint = e.GetPosition(ImageCanvas);
            _drawArrow.StartDrawing(startPoint);
        }
    }

    private void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDrawingArrow && e.LeftButton == MouseButtonState.Pressed)
        {
            Point currentPoint = e.GetPosition(ImageCanvas);
            _drawArrow.UpdateDrawing(currentPoint);
        }
    }

    private void ImageCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDrawingArrow)
        {
            Point endPoint = e.GetPosition(ImageCanvas);
            _drawArrow.EndDrawing(endPoint);
            _isDrawingArrow = false;
        }
    }
}