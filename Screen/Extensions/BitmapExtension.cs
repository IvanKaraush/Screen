using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Screen.Extensions;

public static class BitmapExtension
{
    public static ImageSource ToBitmapSource(this Bitmap bitmap)
    {
        using (var memoryStream = new MemoryStream())
        {
            bitmap.Save(memoryStream, ImageFormat.Bmp);
            memoryStream.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

            bitmapImage.EndInit();
            return bitmapImage;
        }
    }
    
    public static Bitmap ToBitmap(this BitmapSource bitmapSource)
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