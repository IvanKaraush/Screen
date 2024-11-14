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
using Brushes = System.Windows.Media.Brushes;
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
    private bool _printScreenPressed;
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
        if (msg.message == WmHotkey && (int)msg.wParam == HotkeyId && _printScreenPressed == false)
        {
            _printScreenPressed = true;
            foreach (var screen in _screens)
            {
                StartScreenCapture(screen);
            }

            handled = true;
        }
    }

    private BitmapImage MakeScreenshot()
    {
        var screenBounds = _screens[0].Bounds;

        using (var bitmap = new Bitmap(screenBounds.Width, screenBounds.Height))
        {
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(screenBounds.Location, System.Drawing.Point.Empty, screenBounds.Size);
            }

            using (var memoryStream = new System.IO.MemoryStream())
            {
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                memoryStream.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
    }

    public void StartScreenCapture(System.Windows.Forms.Screen screen)
    {
        var overlay = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            WindowState = WindowState.Normal,
            Topmost = true,
            Left = screen.Bounds.Left,
            Top = screen.Bounds.Top,
            Width = screen.Bounds.Width,
            Height = screen.Bounds.Height,
            Cursor = Cursors.Cross,
            Content = new System.Windows.Controls.Image
            {
                Source = MakeScreenshot(),
                Stretch = Stretch.Uniform
            }
        };
        var overlayBlackOut = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Black,
            Opacity = 0.5,
            WindowState = WindowState.Normal,
            Topmost = true,
            Left = screen.Bounds.Left,
            Top = screen.Bounds.Top,
            Width = screen.Bounds.Width,
            Height = screen.Bounds.Height,
            Cursor = Cursors.Cross,
            Content = new TextBlock
            {
                Text = "Выберите область экрана с помощью мыши или нажмите Esc для выхода",
                Foreground = new SolidColorBrush(Colors.White)
            },
        };

        overlay.Show();
        overlayBlackOut.Show();

        overlayBlackOut.MouseLeftButtonUp += Overlay_MouseLeftButtonUp;
        overlayBlackOut.MouseLeftButtonDown += Overlay_MouseLeftButtonDown;
        overlayBlackOut.MouseMove += Overlay_MouseMove;

        overlayBlackOut.KeyUp += HandleEscapeKey;
        _overlays.Add(overlay);
        _overlays.Add(overlayBlackOut);
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
                overlay.Hide();
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
        _printScreenPressed = false;
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
            // todo: Пока так потом поправим
            MessageBox.Show($"Непредвиденная ошибка: {e.Message}");
            throw;
        }
    }

    private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isSelecting)
        {
            return;
        }

        _isSelecting = true;
        var overlay = (Window)sender;

        _startPoint = e.GetPosition(overlay);

        _selectionRectangle = new System.Windows.Shapes.Rectangle
        {
            Fill = Brushes.Transparent,
            Stroke = Brushes.White,
            StrokeThickness = 2
        };

        var canvas = new Canvas();
        canvas.Children.Add(_selectionRectangle);
        overlay.Content = canvas;

        Canvas.SetLeft(_selectionRectangle, _startPoint.X);
        Canvas.SetTop(_selectionRectangle, _startPoint.Y);
        _selectionRectangle.Width = 0;
        _selectionRectangle.Height = 0;
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
        if (!_isSelecting)
        {
            return;
        }

        var overlay = (Window)sender;

        var endPoint = e.GetPosition(overlay);

        var x = Math.Min(_startPoint.X, endPoint.X);
        var y = Math.Min(_startPoint.Y, endPoint.Y);
        var width = Math.Abs(_startPoint.X - endPoint.X);
        var height = Math.Abs(_startPoint.Y - endPoint.Y);

        Canvas.SetLeft(_selectionRectangle, x);
        Canvas.SetTop(_selectionRectangle, y);
        _selectionRectangle.Width = width;
        _selectionRectangle.Height = height;

        if (!_selectionRectangle.IsLoaded)
        {
            var overlayRect = new Rect(0, 0, overlay.Width, overlay.Height);
            var selectionRect = new Rect(x, y, width, height);

            var clipGeometry = new RectangleGeometry(overlayRect);
            var selectionGeometry = new RectangleGeometry(selectionRect);
            var combinedGeometry = new CombinedGeometry(GeometryCombineMode.Exclude, clipGeometry, selectionGeometry);
            overlay.Clip = combinedGeometry;
        }
    }
}