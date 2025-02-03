using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Screen.Extensions;
using Rectangle = System.Windows.Shapes.Rectangle;
using Cursor = System.Windows.Input.Cursor;
using Cursors = System.Windows.Input.Cursors;
using Size = System.Drawing.Size;

namespace Screen.Services;

public class CroppingImageService
{
    private Canvas? _canvas;
    private Bitmap? _originalBitmap;
    private Rectangle? _selectionRectangle;
    private Thumb _leftThumb;
    private Thumb _rightThumb;
    private Thumb _topThumb;
    private Thumb _bottomThumb;

    public void MakeCropping(Canvas? canvas, BitmapSource? previewImage)
    {
        if (canvas == null || previewImage == null)
        {
            return;
        }

        _canvas = canvas;
        _originalBitmap = previewImage.ToBitmap();
        _selectionRectangle = new Rectangle
        {
            StrokeThickness = 2,
            Stroke = new SolidColorBrush(Colors.White),
            Fill = new SolidColorBrush(Colors.Transparent),
            Visibility = Visibility.Visible
        };
        
        CreateAndSetupThumbs();
        PositionSelectionRectangle();
    }
    
    private void CreateAndSetupThumbs()
    {
        if (_canvas == null || _selectionRectangle == null)
        {
            return;
        }
        
        _leftThumb = CreateThumb(Cursors.SizeWE);
        _rightThumb = CreateThumb(Cursors.SizeWE);
        _topThumb = CreateThumb(Cursors.SizeNS);
        _bottomThumb = CreateThumb(Cursors.SizeNS);

        _leftThumb.DragDelta += LeftThumb_DragDelta;
        _rightThumb.DragDelta += RightThumb_DragDelta;
        _topThumb.DragDelta += TopThumb_DragDelta;
        _bottomThumb.DragDelta += BottomThumb_DragDelta;

        _canvas.Children.Add(_selectionRectangle);
        _canvas.Children.Add(_leftThumb);
        _canvas.Children.Add(_rightThumb);
        _canvas.Children.Add(_topThumb);
        _canvas.Children.Add(_bottomThumb);
    }

    private Thumb CreateThumb(Cursor cursor)
    {
        return new Thumb
        {
            Width = 10,
            Height = 10,
            Background = new SolidColorBrush(Colors.Gray),
            Cursor = cursor,
            Visibility = Visibility.Visible
        };
    }

    private void PositionSelectionRectangle()
    {
        if (_selectionRectangle == null)
        {
            return;
        }

        var imageWidth = _originalBitmap?.Width ?? 0;
        var imageHeight = _originalBitmap?.Height ?? 0;

        _selectionRectangle.Width = imageWidth;
        _selectionRectangle.Height = imageHeight;

        Canvas.SetLeft(_selectionRectangle, (imageWidth - _selectionRectangle.Width) / 2);
        Canvas.SetTop(_selectionRectangle, (imageHeight - _selectionRectangle.Height) / 2);

        UpdateThumbPositions();
    }

    private void UpdateThumbPositions()
    {
        if (_selectionRectangle == null)
        {
            return;
        }

        var left = Canvas.GetLeft(_selectionRectangle);
        var top = Canvas.GetTop(_selectionRectangle);
        var width = _selectionRectangle.Width;
        var height = _selectionRectangle.Height;

        Canvas.SetLeft(_leftThumb, left - _leftThumb.Width / 2);
        Canvas.SetTop(_leftThumb, top + height / 2);

        Canvas.SetLeft(_rightThumb, left + width - _rightThumb.Width / 2);
        Canvas.SetTop(_rightThumb, top + height / 2);

        Canvas.SetLeft(_topThumb, left + width / 2);
        Canvas.SetTop(_topThumb, top - _topThumb.Height / 2);

        Canvas.SetLeft(_bottomThumb, left + width / 2);
        Canvas.SetTop(_bottomThumb, top + height - _bottomThumb.Height / 2);
    }

    private void LeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_selectionRectangle == null)
        {
            return;
        }

        var newWidth = _selectionRectangle.Width - e.HorizontalChange;
        var newLeft = Canvas.GetLeft(_selectionRectangle) + e.HorizontalChange;

        if (newWidth > 0 && newLeft >= 0)
        {
            _selectionRectangle.Width = newWidth;
            Canvas.SetLeft(_selectionRectangle, newLeft);

            if (Canvas.GetLeft(_selectionRectangle) < 0)
            {
                Canvas.SetLeft(_selectionRectangle, 0);
            }
        }

        UpdateThumbPositions();
    }

    private void RightThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_selectionRectangle == null || _originalBitmap == null)
        {
            return;
        }

        var newWidth = _selectionRectangle.Width + e.HorizontalChange;

        if (newWidth > 0 && newWidth <= _originalBitmap.Width)
        {
            _selectionRectangle.Width = newWidth;
        }

        UpdateThumbPositions();
    }

    private void TopThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_selectionRectangle == null)
        {
            return;
        }

        var newHeight = _selectionRectangle.Height - e.VerticalChange;
        var newTop = Canvas.GetTop(_selectionRectangle) + e.VerticalChange;

        if (newHeight > 0 && newTop >= 0)
        {
            _selectionRectangle.Height = newHeight;
            Canvas.SetTop(_selectionRectangle, newTop);

            if (Canvas.GetTop(_selectionRectangle) < 0)
            {
                Canvas.SetTop(_selectionRectangle, 0);
            }
        }

        UpdateThumbPositions();
    }

    private void BottomThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_selectionRectangle == null || _originalBitmap == null)
        {
            return;
        }

        var newHeight = _selectionRectangle.Height + e.VerticalChange;

        if (newHeight > 0 && newHeight <= _originalBitmap.Height)
        {
            _selectionRectangle.Height = newHeight;
        }

        UpdateThumbPositions();
    }

    public Bitmap? CropImage()
    {
        if (_selectionRectangle == null || _originalBitmap == null)
        {
            return null;
        }

        var x = (int)Canvas.GetLeft(_selectionRectangle);
        var y = (int)Canvas.GetTop(_selectionRectangle);
        var width = (int)_selectionRectangle.Width;
        var height = (int)_selectionRectangle.Height;

        var croppedBitmap = new Bitmap(width, height);

        using (var graphics = Graphics.FromImage(croppedBitmap))
        {
            var cropArea = new System.Drawing.Rectangle(x, y, width, height);
            graphics.DrawImage(_originalBitmap,
                new System.Drawing.Rectangle(0, 0, width, height), cropArea, GraphicsUnit.Pixel);
        }

        Cleanup();
        return croppedBitmap;
    }

    private void Cleanup()
    {
        if (_canvas != null && _selectionRectangle != null)
        {
            _canvas.Children.Remove(_selectionRectangle);
            _canvas.Children.Remove(_leftThumb);
            _canvas.Children.Remove(_rightThumb);
            _canvas.Children.Remove(_topThumb);
            _canvas.Children.Remove(_bottomThumb);
        }

        _leftThumb.DragDelta -= LeftThumb_DragDelta;
        _rightThumb.DragDelta -= RightThumb_DragDelta;
        _topThumb.DragDelta -= TopThumb_DragDelta;
        _bottomThumb.DragDelta -= BottomThumb_DragDelta;
    }
}