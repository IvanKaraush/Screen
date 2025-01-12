using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Screen.Models;
using System.Windows.Media.Imaging;
using Screen.Extensions;
using Screen.Services;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ComboBox = System.Windows.Controls.ComboBox;
using Cursors = System.Windows.Input.Cursors;
using FlowDirection = System.Windows.FlowDirection;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using TextBox = System.Windows.Controls.TextBox;

namespace Screen;

public partial class MainWindow
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 9000;

    private NotifyIcon _trayIcon;
    private readonly BlurImageService _blurImageService;
    private readonly ScreenCaptureService _screenCaptureService;
    private bool _printScreenPressed;
    private readonly MainViewModel _viewModel;
    private readonly DragService _dragService = new();
    private bool _isAddingTextBox;
    private bool _isAddingTextBoxWithBorder;
    private TextBox? _newTextBox;

    public MainWindow()
    {
        _trayIcon = new NotifyIcon();
        _screenCaptureService = new ScreenCaptureService();
        InitializeComponent();
        InitializeTrayIcon();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _blurImageService = new BlurImageService();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        RegisterHotKey(helper.Handle, HotkeyId, 0, (uint)Keys.PrintScreen);
        ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;
    }

    protected override void OnClosed(EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        UnregisterHotKey(helper.Handle, HotkeyId);
        ComponentDispatcher.ThreadPreprocessMessage -= ComponentDispatcher_ThreadPreprocessMessage;

        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = new Icon(SystemIcons.Application, 40, 40),
            Visible = true,
            Text = "Screen"
        };

        _trayIcon.ContextMenuStrip = new ContextMenuStrip();
        _trayIcon.ContextMenuStrip.Items.Add("Показать", null, ShowWindow);
        _trayIcon.ContextMenuStrip.Items.Add("Выход", null, ExitApplication);

        _trayIcon.DoubleClick += ShowWindow;
    }

    private void ComponentDispatcher_ThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
        if (msg.message == WmHotkey && (int)msg.wParam == HotkeyId && !_printScreenPressed)
        {
            _printScreenPressed = true;
            foreach (var screen in _screenCaptureService.Screens)
            {
                var overlay = _screenCaptureService.StartScreenCapture(screen);
                overlay.MouseLeftButtonUp += Overlay_MouseLeftButtonUp;
                overlay.MouseLeftButtonDown += Overlay_MouseLeftButtonDown;
                overlay.MouseMove += Overlay_MouseMove;

                overlay.KeyUp += HandleEscapeKey;
            }

            handled = true;
        }
    }

    private void HandleEscapeKey(object sender, KeyEventArgs args)
    {
        if (args.Key == Key.Escape)
        {
            foreach (var overlay in _screenCaptureService.Overlays)
            {
                overlay.Close();
            }

            _screenCaptureService.Overlays.Clear();
        }
    }

    private void Overlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ShowPreviewWindow(_screenCaptureService.StopCapture());
    }

    private void ShowPreviewWindow(Bitmap screenshot)
    {
        PreviewImage.Source = screenshot.ToBitmapSource();
        Show();
        _printScreenPressed = false;
    }

    private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var overlay = (Window)sender;
        _screenCaptureService.StartCapture(overlay, e.GetPosition(overlay));
    }

    private void ShowWindow(object? sender, EventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Current.Shutdown();
    }

    private void Overlay_MouseMove(object sender, MouseEventArgs e)
    {
        var overlay = (Window)sender;
        _screenCaptureService.ChangeScreenFigure(overlay, e.GetPosition(overlay));
    }

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var canvas = sender as Canvas;
        var startPoint = e.GetPosition(canvas);
        _viewModel.MouseDown(canvas, startPoint);
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        var canvas = sender as Canvas;
        var currentPoint = e.GetPosition(canvas);
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _viewModel.MouseMove(currentPoint);
        }
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _viewModel.MouseUp();
    }

    private void MakeBlurButton(object sender, RoutedEventArgs e)
    {
        PreviewImage.Cursor = Cursors.Cross;
        PreviewImage.MouseDown += PreviewImageBlur_MouseLeftButtonDown;
        PreviewImage.MouseMove += PreviewImageBlur_MouseLeftButtonMove;
        PreviewImage.MouseUp += PreviewImageBlur_MouseLeftButtonUp;
    }

    private void PreviewImageBlur_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _blurImageService.StartBlurRectangle(e.GetPosition(PreviewImage), PreviewImage.Parent as Canvas);
    }

    private void PreviewImageBlur_MouseLeftButtonMove(object sender, MouseEventArgs e)
    {
        _blurImageService.ProcessedBlurRectangle(e.GetPosition(PreviewImage));
    }

    private void PreviewImageBlur_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        PreviewImage.MouseDown -= PreviewImageBlur_MouseLeftButtonDown;
        PreviewImage.MouseMove -= PreviewImageBlur_MouseLeftButtonMove;
        PreviewImage.MouseUp -= PreviewImageBlur_MouseLeftButtonUp;

        var intensity = (int)BlurIntensitySlider.Value;
        _blurImageService.MakeBlur((BitmapSource)PreviewImage.Source);

        var blurredBitmap = _blurImageService.StopBlurRectangle(PreviewImage.Parent as Canvas, intensity);

        PreviewImage.Source = null;
        PreviewImage.Source = blurredBitmap == null
            ? throw new InvalidOperationException($"{nameof(BitmapImage)} cannot be null")
            : blurredBitmap.ToBitmapSource();
    }

    private void ComboBox_DropDownOpened(object sender, EventArgs e)
    {
        var colorDialog = new ColorDialog();
        if (colorDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        var selectedColor = colorDialog.Color;

        _viewModel.SelectedBrush =
            new SolidColorBrush(Color.FromArgb(selectedColor.A, selectedColor.R, selectedColor.G, selectedColor.B));

        if (sender is ComboBox comboBox)
        {
            comboBox.Items.Clear();
            var rectangle = new Rectangle
            {
                Width = 20,
                Height = 20,
                Fill = _viewModel.SelectedBrush
            };

            comboBox.Items.Add(rectangle);
            comboBox.SelectedIndex = 0;
        }
    }


    private void AddTextBox_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.DisableAllSelectCommands(); 
        _isAddingTextBox = true; 

        DrawingCanvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_isAddingTextBox) return;

        Point clickPosition = e.GetPosition(DrawingCanvas);

        _newTextBox = new TextBox
        {
            Width = 200,
            Height = 30,
            Text = "Введите текст...",
            FontSize = 14,
            Foreground = _viewModel.SelectedBrush,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
        };

        Canvas.SetLeft(_newTextBox, clickPosition.X);
        Canvas.SetTop(_newTextBox, clickPosition.Y);

        _newTextBox.GotFocus += TextBox_GotFocus;
        _newTextBox.LostFocus += TextBox_LostFocus;

        DrawingCanvas.Children.Add(_newTextBox);

        _isAddingTextBox = false;
        DrawingCanvas.MouseLeftButtonDown -= Canvas_MouseLeftButtonDown;
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox { Text: "Введите текст..." } textBox) return;
        textBox.Text = "";
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox || !string.IsNullOrWhiteSpace(textBox.Text)) return;
        textBox.Text = "Введите текст...";
        textBox.Foreground = Brushes.Gray;
    }
    
    private void AddTextWithBorder_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.DisableAllSelectCommands();
        _isAddingTextBoxWithBorder = true;

        DrawingCanvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown_ForBorderTextBox;
    }

    private void Canvas_MouseLeftButtonDown_ForBorderTextBox(object sender, MouseButtonEventArgs e)
    {
        if (!_isAddingTextBoxWithBorder) return;

        Point clickPosition = e.GetPosition(DrawingCanvas);

        var border = new Border
        {
            BorderBrush = _viewModel.SelectedBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.Black,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(2),
            SnapsToDevicePixels = true
        };

        var textBox = new TextBox
        {
            Text = "Введите текст...",
            FontSize = 14,
            Foreground = _viewModel.SelectedBrush,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            AcceptsReturn = true, 
            TextWrapping = TextWrapping.Wrap, 
            MinWidth = 50, 
            MinHeight = 20
        };

        textBox.GotFocus += (s, args) =>
        {
            if (textBox.Text == "Введите текст...")
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.White;
            }
        };

        textBox.LostFocus += (s, args) =>
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Введите текст...";
                textBox.Foreground = Brushes.Gray;
            }
        };

        textBox.TextChanged += (s, args) =>
        {
            textBox.Width = MeasureTextWidth(textBox) + 10; 
            textBox.Height = MeasureTextHeight(textBox) + 4; 
        };

        border.Child = textBox;

        Canvas.SetLeft(border, clickPosition.X);
        Canvas.SetTop(border, clickPosition.Y);

        DrawingCanvas.Children.Add(border);

        textBox.Focus();

        _isAddingTextBoxWithBorder = false;
        DrawingCanvas.MouseLeftButtonDown -= Canvas_MouseLeftButtonDown_ForBorderTextBox;
    }

    [Obsolete("Obsolete")]
    private double MeasureTextWidth(TextBox textBox)
    {
        var formattedText = new FormattedText(
            textBox.Text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
            textBox.FontSize,
            Brushes.Black,
            new NumberSubstitution(),
            TextFormattingMode.Display);

        return formattedText.Width;
    }

    [Obsolete("Obsolete")]
    private double MeasureTextHeight(TextBox textBox)
    {
        var formattedText = new FormattedText(
            textBox.Text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
            textBox.FontSize,
            Brushes.Black,
            new NumberSubstitution(),
            TextFormattingMode.Display);

        return formattedText.Height;
    }

    private void EnableDragButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.DisableAllSelectCommands();
        foreach (var child in DrawingCanvas.Children)
        {
            if (child is UIElement element)
            {
                _dragService.EnableDragging(element);
            }
        }
    }
}