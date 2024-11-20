using System.Windows.Controls;
using System.Windows.Input;
using Screen.Command;
using Screen.Exceptions;
using Screen.Primitives;
using Screen.Services;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace Screen.Models;

public class MainViewModel
{
    private readonly Dictionary<ServiceNames, dynamic> _drawServices;
    private dynamic? _currentDrawService;

    // ReSharper disable once MemberCanBePrivate.Global
    public Brush SelectedBrush { get; set; }

    public ICommand SelectRectangleCommand { get; }
    public ICommand SelectEllipseCommand { get; }
    public ICommand SelectArrowCommand { get; }
    public ICommand SelectLineCommand { get; }

    public MainViewModel()
    {
        SelectedBrush = Brushes.White;

        _drawServices = new Dictionary<ServiceNames, dynamic>
        {
            { ServiceNames.Rectangle, new ClipService() },
            { ServiceNames.Ellipse, new EllipseService() },
            { ServiceNames.Arrow, new DrawArrowService() },
            { ServiceNames.Line, new DrawLineService() }
        };

        SelectRectangleCommand = new RelayCommand(_ => SelectService(ServiceNames.Rectangle));
        SelectEllipseCommand = new RelayCommand(_ => SelectService(ServiceNames.Ellipse));
        SelectArrowCommand = new RelayCommand(_ => SelectService(ServiceNames.Arrow));
        SelectLineCommand = new RelayCommand(_ => SelectService(ServiceNames.Line));
    }

    private void SelectService(ServiceNames key)
    {
        _currentDrawService = _drawServices.TryGetValue(key, out var service)
            ? service
            : throw new ServiceNotFoundException($"Сервиса с ключом {key} не существует");
    }

    public void MouseDown(Canvas? canvas, Point startPoint)
    {
        _currentDrawService?.MouseDown(canvas, startPoint, SelectedBrush);
    }

    public void MouseMove(Point currentPoint)
    {
        _currentDrawService?.MouseMove(currentPoint);
    }

    public void MouseUp()
    {
        _currentDrawService?.MouseUp();
    }
}