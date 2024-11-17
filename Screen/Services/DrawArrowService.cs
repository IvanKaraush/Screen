using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace Screen.Services
{
    public class DrawArrowService
{
    private Line _arrowLine; // Линия (тело стрелки)
    private Polygon _arrowHead; // Треугольник (наконечник стрелки)
    private Point _startPoint;
    private Point _endPoint;

    public void MouseDown(Canvas canvas, Point startPoint, Brush color)
    {
        _startPoint = startPoint;

        // Создаем линию для тела стрелки
        _arrowLine = new Line
        {
            Stroke = color,
            StrokeThickness = 2
        };

        // Создаем треугольник для наконечника стрелки
        _arrowHead = new Polygon
        {
            Fill = color,
            Stroke = color,
            StrokeThickness = 1
        };

        canvas.Children.Add(_arrowLine);
        canvas.Children.Add(_arrowHead);
    }

    public void MouseMove(Canvas canvas, Point currentPoint)
    {
        if (_arrowLine == null || _arrowHead == null) return;

        _endPoint = currentPoint;

        // Обновляем линию (тело стрелки)
        _arrowLine.X1 = _startPoint.X;
        _arrowLine.Y1 = _startPoint.Y;
        _arrowLine.X2 = _endPoint.X;
        _arrowLine.Y2 = _endPoint.Y;

        // Рассчитываем параметры треугольного наконечника
        double arrowHeadLength = 15; // Длина наконечника
        double arrowHeadWidth = 10;  // Ширина основания наконечника
        double angle = Math.Atan2(_endPoint.Y - _startPoint.Y, _endPoint.X - _startPoint.X);

        // Точки треугольного наконечника
        var tipPoint = _endPoint; // Вершина треугольника
        var basePoint1 = new Point(
            _endPoint.X - arrowHeadLength * Math.Cos(angle) + arrowHeadWidth * Math.Sin(angle),
            _endPoint.Y - arrowHeadLength * Math.Sin(angle) - arrowHeadWidth * Math.Cos(angle)
        );
        var basePoint2 = new Point(
            _endPoint.X - arrowHeadLength * Math.Cos(angle) - arrowHeadWidth * Math.Sin(angle),
            _endPoint.Y - arrowHeadLength * Math.Sin(angle) + arrowHeadWidth * Math.Cos(angle)
        );

        // Обновляем треугольник
        _arrowHead.Points = new PointCollection { tipPoint, basePoint1, basePoint2 };
    }

    public void MouseUp()
    {
        // Удаляем текущую временную стрелку, оставляя только конечный результат
        _arrowLine = null;
        _arrowHead = null;
    }
}

}
