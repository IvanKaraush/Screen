using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Screen.Extensions;
using Point = System.Windows.Point;

namespace Screen.Services;

public class CroppingImageService
{
    private System.Windows.Shapes.Rectangle? _selectionRectangle;
    private Point _startPoint;
    private Bitmap? _originalBitmap;

    public Bitmap SetImage(BitmapSource bitmapSource)
    {
        _originalBitmap = bitmapSource.ToBitmap();
        return _originalBitmap;
    }

    public void MouseLeftButtonDown(Point startPoint, Canvas? canvas)
    {
        _startPoint = startPoint;
        _selectionRectangle = new System.Windows.Shapes.Rectangle
        {
            StrokeThickness = 2,
            Stroke = new SolidColorBrush(Colors.White),
            Fill = new SolidColorBrush(Colors.Transparent)
        };
        Canvas.SetLeft(_selectionRectangle, _startPoint.X);
        Canvas.SetTop(_selectionRectangle, _startPoint.Y);
        canvas?.Children.Add(_selectionRectangle);
    }

    public void MouseLeftButtonMove(Point currentPoint)
    {
        if (_selectionRectangle == null)
        {
            return;
        }

        var x = Math.Min(_startPoint.X, currentPoint.X);
        var y = Math.Min(_startPoint.Y, currentPoint.Y);
        var width = Math.Abs(_startPoint.X - currentPoint.X);
        var height = Math.Abs(_startPoint.Y - currentPoint.Y);

        Canvas.SetLeft(_selectionRectangle, x);
        Canvas.SetTop(_selectionRectangle, y);
        _selectionRectangle.Width = width;
        _selectionRectangle.Height = height;
    }

    public Bitmap? MouseLeftButtonUp(Canvas? canvas)
    {
        if (_selectionRectangle == null || _originalBitmap == null)
        {
            return null;
        }

        canvas?.Children.Remove(_selectionRectangle);

        var x = (int)Canvas.GetLeft(_selectionRectangle);
        var y = (int)Canvas.GetTop(_selectionRectangle);
        var width = (int)_selectionRectangle.Width;
        var height = (int)_selectionRectangle.Height;

        _selectionRectangle = null;
        return CropImage(x, y, width, height);
    }

    private Bitmap CropImage(int x, int y, int width, int height)
    {
        if (_originalBitmap == null)
        {
            throw new InvalidOperationException("Image source is not set.");
        }

        var croppedBitmap = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(croppedBitmap))
        {
            var cropArea = new Rectangle(x, y, width, height);
            graphics.DrawImage(_originalBitmap, new Rectangle(0, 0, width, height), cropArea, GraphicsUnit.Pixel);
        }

        return croppedBitmap;
    }
}