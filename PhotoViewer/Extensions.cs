using System.Windows;

namespace PhotoViewer
{
    static class Extensions
    {
        public static bool IsRenderable(this Size size) => size.Width > 0 && size.Height > 0;
        public static bool IsRenderable(this Int32Rect size) => size.Width > 0 && size.Height > 0;
    }
}
