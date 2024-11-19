using System.Windows.Controls;
using Rectangle = System.Windows.Shapes.Rectangle;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using Brush = System.Windows.Media.Brush;

namespace Screen.Services
{
    public class ClipService
    {
        private Rectangle? _currentRectangle;
        private Point _startPoint;

        public void MouseDown(Canvas? canvas, Point startPoint, Brush color, bool isCtrlPressed)
        {
            _startPoint = startPoint;
            _currentRectangle = new Rectangle
            {
                Stroke = color,
                StrokeThickness = 2,
                Fill = isCtrlPressed ? color : Brushes.Transparent
            };
            Canvas.SetLeft(_currentRectangle, _startPoint.X);
            Canvas.SetTop(_currentRectangle, _startPoint.Y);
            canvas?.Children.Add(_currentRectangle);
        }

        public void MouseMove(Point currentPoint)
        {
            if (_currentRectangle == null) return;

            var x = Math.Min(_startPoint.X, currentPoint.X);
            var y = Math.Min(_startPoint.Y, currentPoint.Y);
            var width = Math.Abs(_startPoint.X - currentPoint.X);
            var height = Math.Abs(_startPoint.Y - currentPoint.Y);

            Canvas.SetLeft(_currentRectangle, x);
            Canvas.SetTop(_currentRectangle, y);
            _currentRectangle.Width = width;
            _currentRectangle.Height = height;
        }


        public void MouseUp()
        {
            _currentRectangle = null;
        }
    }
}