using System.Windows.Controls;
using System.Windows.Input;
using Screen.Command;
using Screen.Services;
using Point = System.Windows.Point;

namespace Screen.Models;

public class MainViewModel
{
    private readonly ClipService _clipService = new ClipService();
    private readonly EllipseService _ellipseService = new EllipseService();
    private bool _isDrawingRectangle = false;
    private bool _isDrawingEllipse = false;

    public ICommand DrawRectangleCommand { get; }
    public ICommand DrawEllipseCommand { get; }

    public MainViewModel()
    {
        DrawRectangleCommand = new RelayCommand(_ => _isDrawingRectangle = true);
        DrawEllipseCommand = new RelayCommand(_ => _isDrawingRectangle = false);
    }

    public void MouseDown(Canvas canvas, Point startPoint)
    {
        if (_isDrawingRectangle)
            _clipService.MouseDown(canvas, startPoint);
        else
            _ellipseService.MouseDown(canvas, startPoint);
    }

    public void MouseMove(Canvas canvas, Point currentPoint)
    {
        if (_isDrawingRectangle)
            _clipService.MouseMove(canvas, currentPoint);
        else
            _ellipseService.MouseMove(canvas, currentPoint);
    }

    public void MouseUp()
    {
        if (_isDrawingRectangle)
            _clipService.MouseUp();
        else
            _ellipseService.MouseUp();
    }
}