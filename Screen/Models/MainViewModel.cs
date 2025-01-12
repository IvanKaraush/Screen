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
    private readonly Dictionary<ServiceNamesEnum, dynamic> _drawServices;
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

        _drawServices = new Dictionary<ServiceNamesEnum, dynamic>
        {
            { ServiceNamesEnum.Rectangle, new ClipService() },
            { ServiceNamesEnum.Ellipse, new EllipseService() },
            { ServiceNamesEnum.Arrow, new DrawArrowService() },
            { ServiceNamesEnum.Line, new DrawLineService() },
        };

        SelectRectangleCommand = new RelayCommand(_ => SelectService(ServiceNamesEnum.Rectangle));
        SelectEllipseCommand = new RelayCommand(_ => SelectService(ServiceNamesEnum.Ellipse));
        SelectArrowCommand = new RelayCommand(_ => SelectService(ServiceNamesEnum.Arrow));
        SelectLineCommand = new RelayCommand(_ => SelectService(ServiceNamesEnum.Line));
    }

    private void SelectService(ServiceNamesEnum key)
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

    public void DisableAllSelectCommands()
    {
        _currentDrawService = null;
    }
}