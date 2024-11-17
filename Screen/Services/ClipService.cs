using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using Rectangle = System.Windows.Shapes.Rectangle;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace Screen.Services
{
    public class ClipService
    {
        private Rectangle _currentRectangle;
        private Point _startPoint;

        public void MouseDown(Canvas canvas, System.Windows.Point startPoint)
        {
            _startPoint = startPoint;
            _currentRectangle = new Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            Canvas.SetLeft(_currentRectangle, _startPoint.X);
            Canvas.SetTop(_currentRectangle, _startPoint.Y);
            canvas.Children.Add(_currentRectangle);
        }

        public void MouseMove(Canvas canvas, System.Windows.Point currentPoint)
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
