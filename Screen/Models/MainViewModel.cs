using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using Screen.Command;
using Screen.Services;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace Screen.Models;

public class MainViewModel : INotifyPropertyChanged
{
    private Brush _selectedBrush;
    private bool _isDrawingArrow;
    private bool _isDrawingLine;
    public Brush SelectedBrush
    {
        get => _selectedBrush;
        set
        {
            _selectedBrush = value;
            OnPropertyChanged();
        }
    }

    public ICommand DrawRectangleCommand { get; }
    public ICommand DrawEllipseCommand { get; }
    public ICommand DrawArrowCommand { get; }
    public ICommand DrawLineCommand { get; }

    private bool IsCtrlPressed => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
    
    private readonly ClipService _clipService = new ClipService();
    private readonly EllipseService _ellipseService = new EllipseService();
    private readonly DrawArrowService _drawArrowService = new DrawArrowService();
    private readonly DrawLineService _drawLineService = new DrawLineService();
    private bool _isDrawingRectangle;
    private bool _isDrawingEllipse;
    

    public bool IsDrawingRectangle
    {
        get => _isDrawingRectangle;
        set
        {
            _isDrawingRectangle = value;
            OnPropertyChanged();
        }
    }

    public bool IsDrawingEllipse
    {
        get => _isDrawingEllipse;
        set
        {
            _isDrawingEllipse = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel()
    {
        SelectedBrush = Brushes.Green;

        DrawRectangleCommand = new RelayCommand(_ =>
        {
            IsDrawingRectangle = true;
            IsDrawingEllipse = false;
        });

        DrawEllipseCommand = new RelayCommand(_ =>
        {
            IsDrawingRectangle = false;
            IsDrawingEllipse = true;
        });
        DrawArrowCommand = new RelayCommand(_ =>
        {
            _isDrawingArrow = true;
            _isDrawingLine = false;
        });

        DrawLineCommand = new RelayCommand(_ =>
        {
            _isDrawingArrow = false;
            _isDrawingLine = true;
        });
    }
    
    public void MouseMove(Canvas canvas, Point currentPoint)
    {
        if (_isDrawingRectangle)
        {
            _clipService.MouseMove(canvas, currentPoint);
        }
        else if (IsDrawingEllipse)
        {
            _ellipseService.MouseMove(canvas, currentPoint);
        }
        else if (_isDrawingArrow)
        { 
            _drawArrowService.MouseMove(canvas, currentPoint);
        }
        else if (_isDrawingLine) 
        { 
            _drawLineService.MouseMove(canvas, currentPoint);
        }
    }

    public void MouseUp()
    {
        if (_isDrawingRectangle)
            _clipService.MouseUp();
        else if(IsDrawingEllipse)
            _ellipseService.MouseUp();
        else if (_isDrawingArrow)
            _drawArrowService.MouseUp();
        else if (_isDrawingLine)
            _drawLineService.MouseUp();
    }

    public void MouseDown(Canvas canvas, Point startPoint)
    {
        if (IsDrawingRectangle)
        {
            _clipService.MouseDown(canvas, startPoint, SelectedBrush, IsCtrlPressed);
        }
        else if (IsDrawingEllipse)
        {
            _ellipseService.MouseDown(canvas, startPoint, SelectedBrush, IsCtrlPressed);
        }
        else if (_isDrawingArrow)
        {
            _drawArrowService.MouseDown(canvas, startPoint, SelectedBrush);
        }
        else if (_isDrawingLine)
        {
            _drawLineService.MouseDown(canvas, startPoint, SelectedBrush);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}