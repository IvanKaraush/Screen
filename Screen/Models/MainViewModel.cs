using System.Windows.Controls;
using System.Windows.Input;
using Screen.Command;
using Screen.Services;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace Screen.Models;

public class MainViewModel
{
    private bool _isDrawingArrow;
    private bool _isDrawingLine;
    private bool _isDrawingRectangle;
    private bool _isDrawingEllipse;

    // ReSharper disable once MemberCanBePrivate.Global
    public Brush SelectedBrush { get; set; }

    public ICommand DrawRectangleCommand { get; }
    public ICommand DrawEllipseCommand { get; }
    public ICommand DrawArrowCommand { get; }
    public ICommand DrawLineCommand { get; }


    private bool IsCtrlPressed => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

    private readonly ClipService _clipService = new();
    private readonly EllipseService _ellipseService = new();
    private readonly DrawArrowService _drawArrowService = new();
    private readonly DrawLineService _drawLineService = new();

    private bool IsDrawingRectangle
    {
        get => _isDrawingRectangle;
        set => _isDrawingRectangle = value;
    }

    private bool IsDrawingEllipse
    {
        get => _isDrawingEllipse;
        set => _isDrawingEllipse = value;
    }

    private bool IsDrawingArrow
    {
        get => _isDrawingArrow;
        set => _isDrawingArrow = value;
    }

    private bool IsDrawingLine
    {
        get => _isDrawingLine;
        set => _isDrawingLine = value;
    }

    public MainViewModel()
    {
        SelectedBrush = Brushes.White;

        DrawRectangleCommand = new RelayCommand(_ =>
        {
            ResetDrawingStates();
            IsDrawingRectangle = true;
        });

        DrawEllipseCommand = new RelayCommand(_ =>
        {
            ResetDrawingStates();
            IsDrawingEllipse = true;
        });

        DrawArrowCommand = new RelayCommand(_ =>
        {
            ResetDrawingStates();
            IsDrawingArrow = true;
        });

        DrawLineCommand = new RelayCommand(_ =>
        {
            ResetDrawingStates();
            IsDrawingLine = true;
        });
    }


    private void ResetDrawingStates()
    {
        IsDrawingRectangle = false;
        IsDrawingEllipse = false;
        IsDrawingArrow = false;
        IsDrawingLine = false;
    }


    public void MouseMove(Point currentPoint)
    {
        if (_isDrawingRectangle)
        {
            _clipService.MouseMove(currentPoint);
        }
        else if (_isDrawingEllipse)
        {
            _ellipseService.MouseMove(currentPoint);
        }
        else if (_isDrawingArrow)
        {
            _drawArrowService.MouseMove(currentPoint);
        }
        else if (_isDrawingLine)
        {
            _drawLineService.MouseMove(currentPoint);
        }
    }

    public void MouseUp()
    {
        if (_isDrawingRectangle)
            _clipService.MouseUp();
        else if (_isDrawingEllipse)
            _ellipseService.MouseUp();
        else if (_isDrawingArrow)
            _drawArrowService.MouseUp();
        else if (_isDrawingLine)
            _drawLineService.MouseUp();
    }

    public void MouseDown(Canvas? canvas, Point startPoint)
    {
        if (IsDrawingRectangle)
        {
            _clipService.MouseDown(canvas, startPoint, SelectedBrush, IsCtrlPressed);
        }
        else if (IsDrawingEllipse)
        {
            _ellipseService.MouseDown(canvas, startPoint, SelectedBrush, IsCtrlPressed);
        }
        else if (IsDrawingArrow)
        {
            _drawArrowService.MouseDown(canvas, startPoint, SelectedBrush);
        }
        else if (IsDrawingLine)
        {
            _drawLineService.MouseDown(canvas, startPoint, SelectedBrush);
        }
    }
}