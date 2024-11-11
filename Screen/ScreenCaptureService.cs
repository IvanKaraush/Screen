using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Brush = System.Windows.Media.Brush;
using Cursors = System.Windows.Input.Cursors;

namespace Screen;

public class ScreenCaptureService
{
    public readonly List<Window> _overlays = new();

    public void StartScreenCapture(System.Windows.Forms.Screen screen)
    {
        var overlay = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Black,
            Opacity = 0.5,
            WindowState = WindowState.Normal,
            Topmost = true,
            Left = screen.Bounds.Left,
            Top = screen.Bounds.Top,
            Width = screen.Bounds.Width,
            Height = screen.Bounds.Height,
            Cursor = Cursors.Cross
        };

        var label = new TextBlock
        {
            Text = "Выберите область экрана с помощью мыши или нажмите Esc для выхода",
        };

        overlay.Content = label;
        overlay.Show();
        _overlays.Add(overlay);

        overlay.KeyUp += (_, args) =>
        {
            if (args.Key == Key.Escape)
            {
                foreach (var openOverlay in _overlays)
                {
                    openOverlay.Close();
                }
            }
        };
    }
}