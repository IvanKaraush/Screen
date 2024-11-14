using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Input;
using Rectangle = System.Windows.Shapes.Rectangle;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Screen.Services
{
    internal class ClipService
    {
        private Rectangle rectangle;
        private Canvas canvas;
        private bool isResizing = false;
        private Point startPoint;
        public ClipService(Canvas canvas)
        {
            this.canvas = canvas;
        }
        public void AddRectangle(Point initialPosition, double initialWidth = 100, double initialHeight = 75)
        {
            // Создаем прямоугольник
            rectangle = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                Fill = Brushes.Transparent,
                Width = initialWidth,
                Height = initialHeight
            };

            // Устанавливаем начальную позицию прямоугольника
            Canvas.SetLeft(rectangle, initialPosition.X);
            Canvas.SetTop(rectangle, initialPosition.Y);

            // Добавляем прямоугольник на Canvas
            canvas.Children.Add(rectangle);

            // Подключаем события для изменения размеров
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (rectangle != null && e.LeftButton == MouseButtonState.Pressed)
            {
                startPoint = e.GetPosition(canvas);
                isResizing = true;
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isResizing && rectangle != null)
            {
                var currentPoint = e.GetPosition(canvas);

                var width = Math.Abs(currentPoint.X - startPoint.X);
                var height = Math.Abs(currentPoint.Y - startPoint.Y);

                rectangle.Width = width;
                rectangle.Height = height;

                Canvas.SetLeft(rectangle, Math.Min(currentPoint.X, startPoint.X));
                Canvas.SetTop(rectangle, Math.Min(currentPoint.Y, startPoint.Y));
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isResizing = false;
        }
    }
}
