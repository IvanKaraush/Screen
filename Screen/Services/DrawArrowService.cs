using System.Windows.Shapes;
using System.Windows.Controls;
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;

namespace Screen.Services
{
    public class DrawArrowService
    {
        private Line? _arrowLine;
        private Polygon? _arrowHead;
        private Point _startPoint;
        private Point _endPoint;

        public void MouseDown(Canvas? canvas, Point startPoint, Brush color)
        {
            _startPoint = startPoint;

            _arrowLine = new Line
            {
                Stroke = color,
                StrokeThickness = 2
            };

            _arrowHead = new Polygon
            {
                Fill = color,
                Stroke = color,
                StrokeThickness = 1
            };

            canvas?.Children.Add(_arrowLine);
            canvas?.Children.Add(_arrowHead);
        }

        public void MouseMove(Point currentPoint)
        {
            if (_arrowLine == null || _arrowHead == null) return;

            _endPoint = currentPoint;

            _arrowLine.X1 = _startPoint.X;
            _arrowLine.Y1 = _startPoint.Y;
            _arrowLine.X2 = _endPoint.X;
            _arrowLine.Y2 = _endPoint.Y;

            var arrowHeadLength = 15;
            var arrowHeadWidth = 10;
            var angle = Math.Atan2(_endPoint.Y - _startPoint.Y, _endPoint.X - _startPoint.X);


            var tipPoint = _endPoint;
            var basePoint1 = new Point(
                _endPoint.X - arrowHeadLength * Math.Cos(angle) + arrowHeadWidth * Math.Sin(angle),
                _endPoint.Y - arrowHeadLength * Math.Sin(angle) - arrowHeadWidth * Math.Cos(angle)
            );
            var basePoint2 = new Point(
                _endPoint.X - arrowHeadLength * Math.Cos(angle) - arrowHeadWidth * Math.Sin(angle),
                _endPoint.Y - arrowHeadLength * Math.Sin(angle) + arrowHeadWidth * Math.Cos(angle)
            );

            _arrowHead.Points = [tipPoint, basePoint1, basePoint2];
        }

        public void MouseUp()
        {
            _arrowLine = null;
            _arrowHead = null;
        }
    }
}