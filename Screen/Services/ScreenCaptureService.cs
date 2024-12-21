using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    public IEnumerable<System.Windows.Forms.Screen> Screens { get; } = System.Windows.Forms.Screen.AllScreens;
    private bool _isSelecting;
    private Rectangle? _selectionRectangle;
    private Point _startPoint;
    private TextBlock? _sizeTextBlock;
    private Popup? _textPopup;
    public List<Window> Overlays { get; } = [];

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

    public void MouseLeftButtonDown(Window overlay, Point startPosition)
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

        _sizeTextBlock = new TextBlock
        {
            Foreground = Brushes.Black,
            Background = Brushes.White,
            Margin = new Thickness(5),
            FontSize = 14,
            TextAlignment = TextAlignment.Center
        };

        _textPopup = new Popup
        {
            Child = _sizeTextBlock,
            StaysOpen = true,
            AllowsTransparency = true
        };

        var canvas = new Canvas();
        canvas.Children.Add(_selectionRectangle);
        overlay.Content = canvas;
        overlay.Tag = _sizeTextBlock;

        Canvas.SetLeft(_selectionRectangle, _startPoint.X);
        Canvas.SetTop(_selectionRectangle, _startPoint.Y);
        _selectionRectangle.Width = 0;
        _selectionRectangle.Height = 0;
    }

    public Bitmap MouseLeftButtonUp()
    {
        _isSelecting = false;
        foreach (var overlay in Overlays.Where(c => c.Name == "Black").ToList())
        {
            overlay.Hide();
            overlay.Close();
        }

        // Закрыть и скрыть текстовое окно
        if (_textPopup != null)
        {
            _textPopup.IsOpen = false;
        }

        var screenshot = CaptureScreenshot();

        foreach (var overlay in Overlays)
        {
            overlay.Hide();
            overlay.Close();
        }

        Overlays.Clear();
        _selectionRectangle = null;
        return screenshot;
    }

    public void MouseMove(Window overlay, Point startPosition)
    {
        if (_selectionRectangle == null)
        {
            return;
        }

        var x = Math.Min(_startPoint.X, startPosition.X);
        var y = Math.Min(_startPoint.Y, startPosition.Y);
        var width = Math.Abs(_startPoint.X - startPosition.X);
        var height = Math.Abs(_startPoint.Y - startPosition.Y);

        Canvas.SetLeft(_selectionRectangle, x);
        Canvas.SetTop(_selectionRectangle, y);
        _selectionRectangle.Width = width;
        _selectionRectangle.Height = height;

        UpdateTextPopup(x, y, width, height);

        var overlayBrush = new DrawingBrush
        {
            Drawing = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, overlay.Width, overlay.Height)),
                Brush = Brushes.Black
            }
        };

        overlayBrush.Drawing = new GeometryDrawing
        {
            Geometry = new CombinedGeometry(
                GeometryCombineMode.Exclude,
                new RectangleGeometry(new Rect(0, 0, overlay.Width, overlay.Height)),
                new RectangleGeometry(new Rect(x, y, width, height))
            ),
            Brush = Brushes.Black
        };

        overlay.OpacityMask = overlayBrush;
    }

    private void UpdateTextPopup(double x, double y, double width, double height)
    {
        if (_sizeTextBlock == null || _textPopup == null || _selectionRectangle == null)
        {
            return;
        }

        _sizeTextBlock.Text = $"{(int)width + 1}x{(int)height + 1}";

        var rectPosition = _selectionRectangle.TransformToAncestor(_selectionRectangle.Parent as Visual)
            .Transform(new Point(0, 0));

        _textPopup.HorizontalOffset = rectPosition.X + (width - _sizeTextBlock.ActualWidth) / 2;
        _textPopup.VerticalOffset = rectPosition.Y + (height - _sizeTextBlock.ActualHeight) / 2;

        _textPopup.IsOpen = true;
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