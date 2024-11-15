using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;

namespace Screen.Services
{
    public class DrawArrow
    {
        private readonly Canvas _canvas;
        private Line? _currentArrowLine;
        private Point _startPoint;
        private readonly Brush _selectedColor;

        public DrawArrow(Canvas canvas, Brush color)
        {
            _canvas = canvas;
            _selectedColor = color;
        }

        public void StartDrawing(Point startPoint)
        {
            _startPoint = startPoint;

            _currentArrowLine = new Line
            {
                Stroke = _selectedColor,
                StrokeThickness = 3,
                X1 = startPoint.X,
                Y1 = startPoint.Y,
                X2 = startPoint.X,
                Y2 = startPoint.Y
            };

            _canvas.Children.Add(_currentArrowLine);
            _canvas.CaptureMouse();
        }

        public void UpdateDrawing(Point currentPoint)
        {
            if (_currentArrowLine != null)
            {
                _currentArrowLine.X2 = currentPoint.X;
                _currentArrowLine.Y2 = currentPoint.Y;
            }
        }

        public void EndDrawing(Point endPoint)
        {
            if (_currentArrowLine != null)
            {
                AddArrowHead(_currentArrowLine, endPoint);
                _canvas.ReleaseMouseCapture();
                _currentArrowLine = null;
            }
        }

        private void AddArrowHead(Line line, Point endPoint)
        {
            var direction = new Vector(line.X2 - line.X1, line.Y2 - line.Y1);
            direction.Normalize();

            var arrowHead1 = new Vector(-direction.Y, direction.X) * 10;
            var arrowHead2 = new Vector(direction.Y, -direction.X) * 10;

            var arrowPoint1 = endPoint - direction * 10 + arrowHead1;
            var arrowPoint2 = endPoint - direction * 10 + arrowHead2;

            var headLine1 = new Line
            {
                X1 = endPoint.X,
                Y1 = endPoint.Y,
                X2 = arrowPoint1.X,
                Y2 = arrowPoint1.Y,
                Stroke = _selectedColor,
                StrokeThickness = 3
            };

            var headLine2 = new Line
            {
                X1 = endPoint.X,
                Y1 = endPoint.Y,
                X2 = arrowPoint2.X,
                Y2 = arrowPoint2.Y,
                Stroke = _selectedColor,
                StrokeThickness = 3
            };

            _canvas.Children.Add(headLine1);
            _canvas.Children.Add(headLine2);
        }
    }
}