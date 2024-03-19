using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApp1;

public static class BitmapImageUtility
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PixelColor
    {
        public byte Blue;
        public byte Green;
        public byte Red;
        public byte Alpha;
    }

    public static PixelColor[,] GetPixels(BitmapSource source)
    {
        if(source.Format!=PixelFormats.Bgra32)
            source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

        int width = source.PixelWidth;
        int height = source.PixelHeight;
        PixelColor[,] result = new PixelColor[width, height];

        CopyPixels(source, result, width * 4, 0);
        return result;
    }

    public static void CopyPixels(this BitmapSource source, PixelColor[,] pixels, int stride, int offset)
    {
        var height = source.PixelHeight;
        var width = source.PixelWidth;
        var pixelBytes = new byte[height * width * 4];
        source.CopyPixels(pixelBytes, stride, 0);
        int y0 = offset / width;
        int x0 = offset - width * y0;
        for(int y=0; y<height; y++)
        for(int x=0; x<width; x++)
            pixels[x+x0, y+y0] = new PixelColor
            {
                Blue  = pixelBytes[(y*width + x) * 4 + 0],
                Green = pixelBytes[(y*width + x) * 4 + 1],
                Red   = pixelBytes[(y*width + x) * 4 + 2],
                Alpha = pixelBytes[(y*width + x) * 4 + 3],
            };
    }


    public static BitmapImage BitmapToImageSource(Bitmap bitmap)
    {
        using (MemoryStream memory = new MemoryStream())
        {
            try
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
            catch (AccessViolationException e)
            {
                throw new WarningException(e.Message, e);
            }
        }

    }
}