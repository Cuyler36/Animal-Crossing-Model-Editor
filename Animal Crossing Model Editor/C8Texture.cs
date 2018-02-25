using System.Linq;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using GCNToolkit.Formats.Images;
using GCNToolkit.Formats.Colors;
using GCNToolkit.Utilities;

namespace Animal_Crossing_Model_Editor
{
    public class C8Texture
    {
        public ushort[] Palette;
        public BitmapSource Texture;

        private Bitmap internalBitmap;

        /// <summary>
        /// Creates a C8Texture object. The first 0x20 bytes of "Data" should be the 16 RGB5A3 palette colors.
        /// </summary>
        /// <param name="Data">The raw C8 Texture data</param>
        public C8Texture(byte[] Data, uint Width, uint Height)
        {
            // Get 16 Color RGB5A3 Palette
            Palette = new ushort[16];
            for (int i = 0; i < 16; i++)
            {
                Palette[i] = (ushort)((Data[i * 2] << 8) | Data[i * 2 + 1]);
            }

            // Turn C8 "RGB5A3 Indexed" Texture into C8 Texture
            Data = Data.Skip(0x20).ToArray();

            ushort[] EncodedC8ImageData = new ushort[Width * Height];
            for (int i = 0; i < EncodedC8ImageData.Length / 2; i++)
            {
                EncodedC8ImageData[i * 2] = Palette[(Data[i] >> 4) & 0x0F];
                EncodedC8ImageData[i * 2 + 1] = Palette[Data[i] & 0x0F];
            }

            // Decode C8 Texture
            ushort[] DecodedC8ImageData = C8.DecodeC8(EncodedC8ImageData, Width, Height);

            // Generate Bitmap Data from C8 Texture
            byte[] BitmapImageData = new byte[DecodedC8ImageData.Length * 4];

            for (int i = 0; i < DecodedC8ImageData.Length; i++)
            {
                int idx = i * 4;
                RGB5A3.ToARGB8(DecodedC8ImageData[i], out byte A, out byte R, out byte G, out byte B);
                BitmapImageData[idx] = A;
                BitmapImageData[idx + 1] = R;
                BitmapImageData[idx + 2] = G;
                BitmapImageData[idx + 3] = B;
            }

            // Create Bitmap & BitmapSource from BitmapData
            internalBitmap = BitmapUtilities.CreateBitmap(BitmapImageData, Width, Height);
            Texture = internalBitmap.ToBitmapSource();
        }

        public void SaveTexture(string Path)
        {
            if (internalBitmap != null)
            {
                try
                {
                    internalBitmap.Save(Path);
                }
                catch
                {
                    MessageBox.Show("Unable to save the texture!", "Texture Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
