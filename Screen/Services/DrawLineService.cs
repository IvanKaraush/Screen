using System.Windows.Shapes;
using System.Windows.Controls;
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;

namespace Screen.Services;

public class DrawLineService
{
    private Line? _currentLine;

    public void MouseDown(Canvas? canvas, Point startPoint, Brush color)
    {
        _currentLine = new Line
        {
            Stroke = color,
            StrokeThickness = 2,
            X1 = startPoint.X,
            Y1 = startPoint.Y,
            X2 = startPoint.X,
            Y2 = startPoint.Y
        };

        canvas?.Children.Add(_currentLine);
    }

    public void MouseMove(Point currentPoint)
    {
        if (_currentLine == null) return;

        _currentLine.X2 = currentPoint.X;
        _currentLine.Y2 = currentPoint.Y;
    }

    public void MouseUp()
    {
        _currentLine = null;
    }
}