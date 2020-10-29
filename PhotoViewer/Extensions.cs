using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PhotoViewer
{
    static class Extensions
    {
        public static bool IsRenderable(this Size size) => size.Width > 0 && size.Height > 0;
        public static bool IsRenderable(this Int32Rect size) => size.Width > 0 && size.Height > 0;
        public static byte[] GetBytes(this BitmapSource bitmapSource)
        {
            byte[] result;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(memoryStream);
                memoryStream.Position = 0;
                result = memoryStream.ToArray();
            }

            return result;
        }
    }
}
