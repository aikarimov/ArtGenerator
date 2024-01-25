using ArtModel.ColorModel;
using ArtModel.ColorModel.ColorSpaces;
using System.Drawing;
using System.Drawing.Imaging;

namespace ArtModel.Image.Matrix
{
    public static class MatrixConverter
    {
        public static MatrixBitmap BitmapToMatrix(Bitmap bitmapRGB, ColorSpaceType type)
        {
            int width = bitmapRGB.Width;
            int height = bitmapRGB.Height;
            MatrixBitmap matrixRGB = new MatrixBitmap(width, height, type);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = bitmapRGB.GetPixel(x, y);
                    matrixRGB[x, height - 1 - y][0] = pixel.R;
                    matrixRGB[x, height - 1 - y][1] = pixel.G;
                    matrixRGB[x, height - 1 - y][2] = pixel.B;
                }
            }

            return matrixRGB.FromRgb(type);
        }

        public static void WriteMatrixToFile(MatrixBitmap matrix, string path, string filename, ImageFormat format)
        {
            MatrixBitmap rgbMatrix = matrix.ToRgb();

            int width = matrix.Width;
            int height = matrix.Height;

            using (Bitmap bitmap = new Bitmap(width, height))
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        byte R = Math.Clamp(rgbMatrix[x, height - 1 - y][0], (byte)0, (byte)255);
                        byte G = Math.Clamp(rgbMatrix[x, height - 1 - y][1], (byte)0, (byte)255);
                        byte B = Math.Clamp(rgbMatrix[x, height - 1 - y][2], (byte)0, (byte)255);
                        Color pixel = Color.FromArgb(255, R, G, B);

                        bitmap.SetPixel(x, y, pixel);
                    }
                }

                bitmap.Save(path + $"\\{filename}.{format}");
            }
        }
    }
}
