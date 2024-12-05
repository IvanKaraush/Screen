using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Screen.Services;

public class DragService
{
    private UIElement? _currentElement;
    private Point _lastMousePosition;
    private bool _isDragging;

    public void EnableDragging(UIElement element)
    {
        element.MouseLeftButtonDown += Element_MouseLeftButtonDown;
        element.MouseMove += Element_MouseMove;
        element.MouseLeftButtonUp += Element_MouseLeftButtonUp;
    }

    private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is UIElement element)
        {
            _currentElement = element;
            _lastMousePosition = e.GetPosition(GetParentCanvas(element));
            _isDragging = true;

            element.CaptureMouse();
        }
    }

    private void Element_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && _currentElement != null)
        {
            var canvas = GetParentCanvas(_currentElement);
            var currentMousePosition = e.GetPosition(canvas);

            var offsetX = currentMousePosition.X - _lastMousePosition.X;
            var offsetY = currentMousePosition.Y - _lastMousePosition.Y;

            var newLeft = Canvas.GetLeft(_currentElement) + offsetX;
            var newTop = Canvas.GetTop(_currentElement) + offsetY;

            Canvas.SetLeft(_currentElement, newLeft);
            Canvas.SetTop(_currentElement, newTop);

            _lastMousePosition = currentMousePosition;
        }
    }

    private void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging && _currentElement != null)
        {
            _isDragging = false;
            _currentElement.ReleaseMouseCapture();
            _currentElement = null;
        }
    }

    private Canvas GetParentCanvas(UIElement element)
    {
        var parent = VisualTreeHelper.GetParent(element);
        while (parent != null && !(parent is Canvas))
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        return parent as Canvas ?? throw new InvalidOperationException("The parent is not a Canvas.");
    }
}