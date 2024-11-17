using System.Windows.Controls;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace Screen.Services;

public class EllipseService
{
    private Ellipse _currentEllipse;
    private Point _startPoint;

    public void MouseDown(Canvas canvas, Point startPoint, Brush color, bool isCtrlPressed)
    {
        _startPoint = startPoint;
        _currentEllipse = new Ellipse()
        {
            Stroke = color,
            StrokeThickness = 2,
            Fill = isCtrlPressed ? color : Brushes.Transparent
        };
        Canvas.SetLeft(_currentEllipse, _startPoint.X);
        Canvas.SetTop(_currentEllipse, _startPoint.Y);
        canvas.Children.Add(_currentEllipse);
    }



    public void MouseMove(Canvas canvas, System.Windows.Point currentPoint)
    {
        if (_currentEllipse == null) return;

        var x = Math.Min(_startPoint.X, currentPoint.X);
        var y = Math.Min(_startPoint.Y, currentPoint.Y);
        var width = Math.Abs(_startPoint.X - currentPoint.X);
        var height = Math.Abs(_startPoint.Y - currentPoint.Y);

        Canvas.SetLeft(_currentEllipse, x);
        Canvas.SetTop(_currentEllipse, y);
        _currentEllipse.Width = width;
        _currentEllipse.Height = height;
    }


    public void MouseUp()
    {
        _currentEllipse = null;
    }
}