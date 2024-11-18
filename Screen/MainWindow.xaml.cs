using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Screen.Extensions;
using Screen.Services;
using Application = System.Windows.Application;
using Cursors = System.Windows.Input.Cursors;
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
    private readonly BlurImageService _blurImageService;
    private readonly ScreenCaptureService _screenCaptureService;
    private readonly ClipService _clipService;
    private bool _printScreenPressed;

    public MainWindow()
    {
        _trayIcon = new NotifyIcon();
        _screenCaptureService = new ScreenCaptureService();
        _clipService = new ClipService(ImageCanvas);
        InitializeComponent();
        InitializeTrayIcon();
        _blurImageService = new BlurImageService();
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
        if (msg.message == WmHotkey && (int)msg.wParam == HotkeyId && _printScreenPressed == false)
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
        PreviewImage.Source = screenshot.ToBitmapSource();
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

    private void AddRectangleButton_Click(object sender, RoutedEventArgs e)
    {
        var startPosition = new Point(50, 50);
        _clipService.AddRectangle(startPosition);
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
    }

    private void MakeBlurButton(object sender, RoutedEventArgs e)
    {
        PreviewImage.Cursor = Cursors.Cross;
        PreviewImage.MouseDown += PreviewImageBlur_MouseLeftButtonDown;
        PreviewImage.MouseMove += PreviewImageBlur_MouseLeftButtonMove;
        PreviewImage.MouseUp += PreviewImageBlur_MouseLeftButtonUp;
    }

    private void PreviewImageBlur_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _blurImageService.StartBlurRectangle(e.GetPosition(PreviewImage), PreviewImage.Parent as Canvas);
    }

    private void PreviewImageBlur_MouseLeftButtonMove(object sender, MouseEventArgs e)
    {
        _blurImageService.ProcessedBlurRectangle(e.GetPosition(PreviewImage));
    }

    private void PreviewImageBlur_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        PreviewImage.MouseDown -= PreviewImageBlur_MouseLeftButtonDown;
        PreviewImage.MouseMove -= PreviewImageBlur_MouseLeftButtonMove;
        PreviewImage.MouseUp -= PreviewImageBlur_MouseLeftButtonUp;

        var intensity = (int)BlurIntensitySlider.Value;
        _blurImageService.MakeBlur((BitmapSource)PreviewImage.Source);

        var blurredBitmap = _blurImageService.StopBlurRectangle(PreviewImage.Parent as Canvas, intensity);

        PreviewImage.Source = null;
        PreviewImage.Source = blurredBitmap == null
            ? throw new InvalidOperationException($"{nameof(BitmapImage)} cannot be null")
            : blurredBitmap.ToBitmapSource();
    }
}