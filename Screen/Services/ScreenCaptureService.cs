using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using Cursor = System.Windows.Forms.Cursor;
using Cursors = System.Windows.Input.Cursors;
using MessageBox = System.Windows.MessageBox;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Drawing.Size;

namespace Screen.Services;

public class ScreenCaptureService
{
    public System.Windows.Forms.Screen[] Screens { get; } = System.Windows.Forms.Screen.AllScreens;
    private bool _isSelecting;
    private Rectangle? _selectionRectangle;
    private Point _startPoint;
    public List<Window> Overlays { get; } =  [];

    public Window StartScreenCapture(System.Windows.Forms.Screen screen)
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
                Source = MakeScreenshot(screen),
                Stretch = Stretch.Uniform
            }
        };
        var overlayBlackOut = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Black,
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
            Name = "Black"
        };

        overlay.Show();
        overlayBlackOut.Show();

        Overlays.Add(overlay);
        Overlays.Add(overlayBlackOut);
        return overlayBlackOut;
    }

    public void StartCapture(Window overlay, Point startPosition)
    {
        if (_isSelecting)
        {
            return;
        }

        _isSelecting = true;
        _startPoint = startPosition;
        _selectionRectangle = new Rectangle
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

    public Bitmap StopCapture()
    {
        _isSelecting = false;
        foreach (var overlay in Overlays.Where(c => c.Name == "Black").ToList())
        {
            overlay.Hide();
            overlay.Close();
        }

        var screenshot = CaptureScreenshot();

        foreach (var overlay in Overlays)
        {
            overlay.Hide();
            overlay.Close();
        }

        Overlays.Clear();
        return screenshot;
    }

    public void ChangeScreenFigure(Window overlay, Point startPosition)
    {
        if (!_isSelecting)
        {
            return;
        }

        var endPoint = startPosition;

        var x = Math.Min(_startPoint.X, endPoint.X);
        var y = Math.Min(_startPoint.Y, endPoint.Y);
        var width = Math.Abs(_startPoint.X - endPoint.X);
        var height = Math.Abs(_startPoint.Y - endPoint.Y);
        if (_selectionRectangle == null)
        {
            return;
        }

        Canvas.SetLeft(_selectionRectangle, x);
        Canvas.SetTop(_selectionRectangle, y);
        _selectionRectangle.Width = width;
        _selectionRectangle.Height = height;

        if (_selectionRectangle.IsLoaded)
        {
            return;
        }

        var overlayRect = new Rect(0, 0, overlay.Width, overlay.Height);
        var selectionRect = new Rect(x, y, width, height);

        var clipGeometry = new RectangleGeometry(overlayRect);
        var selectionGeometry = new RectangleGeometry(selectionRect);
        var combinedGeometry =
            new CombinedGeometry(GeometryCombineMode.Exclude, clipGeometry, selectionGeometry);
        overlay.Clip = combinedGeometry;
    }

    private Bitmap CaptureScreenshot()
    {
        try
        {
            var screen = System.Windows.Forms.Screen.FromPoint(Cursor.Position);

            if (_selectionRectangle != null)
            {
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
                        new Size(selectionWidth, selectionHeight)
                    );
                }

                return screenshot;
            }
        }
        catch (Exception e)
        {
            // todo: Пока так потом поправим
            MessageBox.Show($"Непредвиденная ошибка: {e.Message}");
            throw;
        }

        throw new NullReferenceException("Выбранная фигура не может быть null");
    }

    private BitmapImage MakeScreenshot(System.Windows.Forms.Screen screen)
    {
        using (var bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height))
        {
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(screen.Bounds.Location, System.Drawing.Point.Empty, screen.Bounds.Size);
            }

            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Png);

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
}