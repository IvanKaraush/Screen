using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace Screen;

public partial class MainWindow
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 9000;

    private readonly System.Windows.Forms.Screen[] _screens = System.Windows.Forms.Screen.AllScreens;
    private NotifyIcon _trayIcon;
    private bool _isSelecting;
    private System.Windows.Shapes.Rectangle _selectionRectangle;
    private Point _startPoint;
    public readonly List<Window> _overlays = new();

    public MainWindow()
    {
        _trayIcon = new NotifyIcon();
        InitializeComponent();
        InitializeTrayIcon();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        RegisterHotKey(helper.Handle, HotkeyId, 0, (uint)Keys.PrintScreen);
        ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;
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
        if (msg.message == WmHotkey && (int)msg.wParam == HotkeyId)
        {
            foreach (var screen in _screens)
            {
                StartScreenCapture(screen);
            }

            handled = true;
        }
    }

    public void StartScreenCapture(System.Windows.Forms.Screen screen)
    {
        var overlay = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Black,
            Opacity = 0.5,
            WindowState = WindowState.Normal,
            Topmost = true,
            Left = screen.Bounds.Left, // Позиция окна на экране
            Top = screen.Bounds.Top,
            Width = screen.Bounds.Width,
            Height = screen.Bounds.Height,
            Cursor = Cursors.Cross,
            Content = new TextBlock
            {
                Text = "Выберите область экрана с помощью мыши или нажмите Esc для выхода",
                Foreground = new SolidColorBrush(Colors.White)
            }
        };

        overlay.Show();
        _overlays.Add(overlay);

        overlay.MouseLeftButtonUp += Overlay_MouseLeftButtonUp;
        overlay.MouseLeftButtonDown += Overlay_MouseLeftButtonDown;
        overlay.MouseMove += Overlay_MouseMove;

        overlay.KeyUp += HandleEscapeKey;
    }

    private void HandleEscapeKey(object sender, KeyEventArgs args)
    {
        if (args.Key == Key.Escape)
        {
            foreach (var overlay in _overlays)
            {
                overlay.Close();
            }

            _overlays.Clear();
        }
    }

    private void Overlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isSelecting = false;

        Dispatcher.BeginInvoke(new Action(() =>
        {
            foreach (var overlay in _overlays)
            {
                overlay.Close();
            }
            _overlays.Clear();

            var screenshot = CaptureScreenshot();
            ShowPreviewWindow(screenshot);
        }), DispatcherPriority.Loaded);
    }

    private void ShowPreviewWindow(Bitmap screenshot)
    {
        PreviewImage.Source = BitmapToImageSource(screenshot);
        Show();
    }

    private ImageSource BitmapToImageSource(Bitmap bitmap)
    {
        using (var memoryStream = new MemoryStream())
        {
            bitmap.Save(memoryStream, ImageFormat.Bmp);
            memoryStream.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

            bitmapImage.EndInit();
            return bitmapImage;
        }
    }

    private Bitmap CaptureScreenshot()
    {
        try
        {
            var screen = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);

            var selectionX = (int)(Canvas.GetLeft(_selectionRectangle) + screen.Bounds.Left);
            var selectionY = (int)(Canvas.GetTop(_selectionRectangle) + screen.Bounds.Top);
            var selectionWidth = (int)_selectionRectangle.ActualWidth;
            var selectionHeight = (int)_selectionRectangle.ActualHeight;

            var screenshot = new Bitmap(selectionWidth, selectionHeight, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(
                    new System.Drawing.Point(selectionX, selectionY),
                    System.Drawing.Point.Empty,
                    new System.Drawing.Size(selectionWidth, selectionHeight)
                );
            }

            return screenshot;
        }
        catch (Exception e)
        {
            MessageBox.Show($"Непредвиденная ошибка: {e.Message}");
            throw;
        }
    }

    private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isSelecting = true;
        var overlay = (Window)sender;
    
        // Получение начальной точки для выделения
        _startPoint = e.GetPosition(overlay);

        // Создание прямоугольника для выделенной области
        _selectionRectangle = new System.Windows.Shapes.Rectangle
        {
            Fill = System.Windows.Media.Brushes.Transparent,
            Stroke = System.Windows.Media.Brushes.White,
            StrokeThickness = 2
        };

        // Добавляем прямоугольник в контейнер
        var canvas = new Canvas();
        canvas.Children.Add(_selectionRectangle);
        overlay.Content = canvas;

        // Установка маски для окна
        var clipGeometry = new RectangleGeometry(new Rect(0, 0, overlay.Width, overlay.Height));
        var selectionGeometry = new RectangleGeometry(new Rect(_startPoint, new Size(0, 0)));
        var combinedGeometry = new CombinedGeometry(GeometryCombineMode.Exclude, clipGeometry, selectionGeometry);
        overlay.Clip = combinedGeometry;
    }

    // Проверка, на каком экране находится окно (overlay)
    private bool IsOverlayOnScreen(Window overlay, System.Windows.Forms.Screen screen)
    {
        var overlayLeft = overlay.Left;
        var overlayTop = overlay.Top;
        var overlayRight = overlayLeft + overlay.Width;
        var overlayBottom = overlayTop + overlay.Height;

        var screenLeft = screen.Bounds.Left;
        var screenTop = screen.Bounds.Top;
        var screenRight = screen.Bounds.Right;
        var screenBottom = screen.Bounds.Bottom;

        return overlayLeft >= screenLeft && overlayTop >= screenTop &&
               overlayRight <= screenRight && overlayBottom <= screenBottom;
    }

    private void UpdateSelectionRectangle(Point end)
    {
        var x = Math.Min(_startPoint.X, end.X);
        var y = Math.Min(_startPoint.Y, end.Y);
        var width = Math.Abs(_startPoint.X - end.X);
        var height = Math.Abs(_startPoint.Y - end.Y);

        _selectionRectangle.Width = width;
        _selectionRectangle.Height = height;

        Canvas.SetLeft(_selectionRectangle, x);
        Canvas.SetTop(_selectionRectangle, y);
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

    protected override void OnClosed(EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        UnregisterHotKey(helper.Handle, HotkeyId);
        ComponentDispatcher.ThreadPreprocessMessage -= ComponentDispatcher_ThreadPreprocessMessage;

        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }

    private void Overlay_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isSelecting)
        {
            var endPoint = e.GetPosition((UIElement)sender);
            UpdateSelectionRectangle(endPoint);
        
            // Обновление геометрии маски при изменении выделенной области
            var overlay = (Window)sender;
            var clipGeometry = new RectangleGeometry(new Rect(0, 0, overlay.Width, overlay.Height));
            var selectionGeometry = new RectangleGeometry(new Rect(_startPoint, new Size(_selectionRectangle.Width, _selectionRectangle.Height)));
            var combinedGeometry = new CombinedGeometry(GeometryCombineMode.Exclude, clipGeometry, selectionGeometry);
            overlay.Clip = combinedGeometry;
        }
    }
}