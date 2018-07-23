using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Animal_Crossing_Model_Editor
{
    public static class Util
    {
        public static BitmapSource ToImage(byte[] Array, int Width, int Height)
        {
            return BitmapSource.Create(Width, Height, 96, 96, PixelFormats.Bgra32, null, Array, Width * 4);
        }
    }
}
