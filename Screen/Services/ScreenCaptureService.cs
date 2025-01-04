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
            Foreground = Brushes.White,
            Background = Brushes.Black,
            FontSize = 14
        };

        _selectionRectangle.SizeChanged += (_, _) => { UpdateSizeTextBlockPosition(); };
        _sizeTextBlock.SizeChanged += (_, _) => { UpdateSizeTextBlockPosition(); };

        var canvas = new Canvas();
        canvas.Children.Add(_selectionRectangle);
        canvas.Children.Add(_sizeTextBlock);

        overlay.Content = canvas;

        Canvas.SetLeft(_sizeTextBlock, _selectionRectangle.Width - _sizeTextBlock.ActualWidth);
        Canvas.SetTop(_sizeTextBlock, _selectionRectangle.Height - _sizeTextBlock.ActualHeight);
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

    public void MouseMove(Point currentPosition)
    {
        if (_selectionRectangle == null || _sizeTextBlock == null)
        {
            return;
        }

        var x = Math.Min(_startPoint.X, currentPosition.X);
        var y = Math.Min(_startPoint.Y, currentPosition.Y);
        var width = Math.Abs(_startPoint.X - currentPosition.X);
        var height = Math.Abs(_startPoint.Y - currentPosition.Y);

        Canvas.SetLeft(_selectionRectangle, x);
        Canvas.SetTop(_selectionRectangle, y);
        _selectionRectangle.Width = width;
        _selectionRectangle.Height = height;

        UpdateTextPopup(width, height);
        _sizeTextBlock.Text = $"{(int)width}x{(int)height}";
    }

    private void UpdateTextPopup(double width, double height)
    {
        if (_sizeTextBlock == null || _textPopup == null || _selectionRectangle == null)
        {
            return;
        }

        _sizeTextBlock.Text = $"{(int)width + 1}x{(int)height + 1}";

        if (_selectionRectangle.Parent is Visual parentVisual)
        {
            var rectPosition = _selectionRectangle
                .TransformToAncestor(parentVisual)
                .Transform(new Point(0, 0));

            _textPopup.HorizontalOffset = rectPosition.X + (width - _sizeTextBlock.ActualWidth) / 2;
            _textPopup.VerticalOffset = rectPosition.Y + (height - _sizeTextBlock.ActualHeight) / 2;

            _textPopup.IsOpen = true;
        }
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

    private void UpdateSizeTextBlockPosition()
    {
        if (_selectionRectangle == null || _sizeTextBlock == null)
        {
            return;
        }

        var left = Canvas.GetLeft(_selectionRectangle) + _selectionRectangle.Width / 2 -
                   _sizeTextBlock.ActualWidth / 2;
        var top = Canvas.GetTop(_selectionRectangle) + _selectionRectangle.Height / 2 -
                  _sizeTextBlock.ActualHeight / 2;

        Canvas.SetLeft(_sizeTextBlock, left);
        Canvas.SetTop(_sizeTextBlock, top);
    }

    private static BitmapImage MakeScreenshot(System.Windows.Forms.Screen screen)
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