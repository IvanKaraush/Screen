using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image = System.Drawing.Image;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Windows.Point;

namespace Screen.Services;

public class BlurImageService
{
    private System.Windows.Shapes.Rectangle? _selectionRectangle;
    private Point _startPoint;
    private Bitmap? _originalBitmap;

    public Bitmap MakeBlur(BitmapSource previewImage)
    {
        _originalBitmap = BitmapFromImageSource(previewImage);
        return _originalBitmap;
    }

    public void MouseLeftButtonDown(Point startPoint, Canvas? canvas)
    {
        _startPoint = startPoint;
        _selectionRectangle = new System.Windows.Shapes.Rectangle
        {
            StrokeThickness = 2,
            Stroke = new SolidColorBrush(Colors.White)
        };
        Canvas.SetLeft(_selectionRectangle, _startPoint.X);
        Canvas.SetTop(_selectionRectangle, _startPoint.Y);
        canvas?.Children.Add(_selectionRectangle);
    }

    public void MouseLeftButtonMove(Point currentPoint)
    {
        if (_selectionRectangle == null)
        {
            return;
        }

        var x = Math.Min(_startPoint.X, currentPoint.X);
        var y = Math.Min(_startPoint.Y, currentPoint.Y);
        var width = Math.Abs(_startPoint.X - currentPoint.X);
        var height = Math.Abs(_startPoint.Y - currentPoint.Y);

        Canvas.SetLeft(_selectionRectangle, x);
        Canvas.SetTop(_selectionRectangle, y);
        _selectionRectangle.Width = width;
        _selectionRectangle.Height = height;
    }

    public Bitmap? MouseLeftButtonUp(Canvas? canvas, int blurIntensity)
    {
        if (_selectionRectangle == null)
        {
            return default;
        }

        canvas?.Children.Remove(_selectionRectangle);

        // Координаты выделенной области
        var x = (int)Canvas.GetLeft(_selectionRectangle);
        var y = (int)Canvas.GetTop(_selectionRectangle);
        var width = (int)_selectionRectangle.Width;
        var height = (int)_selectionRectangle.Height;

        // Применяем размытие
        _selectionRectangle = null;
        return ApplyBlurToSelection(x, y, width, height, blurIntensity);
    }

    private Bitmap ApplyBlurToSelection(int x, int y, int width, int height, int blurIntensity)
    {
        var blurredBitmap = new Bitmap(_originalBitmap ?? throw new InvalidOperationException($"{nameof(_originalBitmap)} cannot be null"));

        var rect = new Rectangle(x, y, width, height);

        // Рассчитываем радиус размытия на основе интенсивности
        var radius = Math.Max(1, blurIntensity / 10); // Например, при интенсивности 100 радиус = 10
        var kernelSize = 2 * radius + 1;
        var kernel = CreateGaussianKernel(kernelSize, radius);

        var bitmapData = blurredBitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        var bytesPerPixel = Image.GetPixelFormatSize(blurredBitmap.PixelFormat) / 8;
        var stride = bitmapData.Stride;
        var ptr = bitmapData.Scan0;
        var pixelBuffer = new byte[stride * rect.Height];
        Marshal.Copy(ptr, pixelBuffer, 0, pixelBuffer.Length);

        var resultBuffer = new byte[pixelBuffer.Length];
        ApplyGaussianBlur(pixelBuffer, resultBuffer, rect.Width, rect.Height, stride, bytesPerPixel, kernel, kernelSize);

        Marshal.Copy(resultBuffer, 0, ptr, resultBuffer.Length);
        blurredBitmap.UnlockBits(bitmapData);

        return blurredBitmap;
    }

    // Генерация ядра Гаусса
    private static double[,] CreateGaussianKernel(int size, int radius)
    {
        var kernel = new double[size, size];
        var sigma = radius / 2.0;
        var sigma2 = 2 * sigma * sigma;
        var sqrtSigmaPi2 = Math.Sqrt(Math.PI * sigma2);
        var radius2 = radius * radius;

        var total = 0.0;
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                double distance = x * x + y * y;
                if (distance > radius2) continue;

                kernel[x + radius, y + radius] = Math.Exp(-distance / sigma2) / sqrtSigmaPi2;
                total += kernel[x + radius, y + radius];
            }
        }

        // Нормализация ядра
        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                kernel[x, y] /= total;
            }
        }

        return kernel;
    }

    // Применение Гауссова размытия
    private static void ApplyGaussianBlur(byte[] sourceBuffer, byte[] resultBuffer, int width, int height, int stride, int bytesPerPixel,
        double[,] kernel,
        int kernelSize)
    {
        var radius = kernelSize / 2;
        for (var y = radius; y < height - radius; y++)
        {
            for (var x = radius; x < width - radius; x++)
            {
                var blurredPixel = new double[3]; // RGB

                for (var ky = -radius; ky <= radius; ky++)
                {
                    for (var kx = -radius; kx <= radius; kx++)
                    {
                        var pixelX = x + kx;
                        var pixelY = y + ky;
                        var offset = (pixelY * stride) + (pixelX * bytesPerPixel);

                        var weight = kernel[ky + radius, kx + radius];
                        blurredPixel[0] += sourceBuffer[offset] * weight; // Blue
                        blurredPixel[1] += sourceBuffer[offset + 1] * weight; // Green
                        blurredPixel[2] += sourceBuffer[offset + 2] * weight; // Red
                    }
                }

                var resultOffset = (y * stride) + (x * bytesPerPixel);
                resultBuffer[resultOffset] = (byte)blurredPixel[0];
                resultBuffer[resultOffset + 1] = (byte)blurredPixel[1];
                resultBuffer[resultOffset + 2] = (byte)blurredPixel[2];
                resultBuffer[resultOffset + 3] = 255; // Alpha
            }
        }
    }

    private static Bitmap BitmapFromImageSource(BitmapSource bitmapSource)
    {
        using (var memoryStream = new MemoryStream())
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);

            return new Bitmap(memoryStream);
        }
    }
}