using ArtModel.ColorModel;
using ArtModel.ColorModel.ColorSpaces;
using System.Drawing;
using System.Drawing.Imaging;

namespace ArtModel.Image
{
    internal static class MatrixConverter
    {
        public static MatrixBitmap BitmapToMatrix(ColorSpaceType type, Bitmap bitmapRGB)
        {
            int width = bitmapRGB.Width;
            int height = bitmapRGB.Height;
            MatrixBitmap matrix = new MatrixBitmap(height, width, ColorSpaceType.RGB);
            InitializeMatrix(matrix);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = bitmapRGB.GetPixel(x, y);
                    matrix[x, y].Set(0, pixel.R);
                    matrix[x, y].Set(1, pixel.G);
                    matrix[x, y].Set(2, pixel.B);
                }
            }

            return ColorSpaceFormats.ColorSpaces[type].FromRGB(matrix);
        }

        public static void WriteMatrixToFile(MatrixBitmap matrix, string path, string filename, ImageFormat format)
        {
            MatrixBitmap rgbMatrix = ColorSpaceFormats.ColorSpaces[matrix.ColorSpace].ToRGB(matrix);

            int width = matrix.Columns;
            int height = matrix.Rows;
            Bitmap bitmap = new Bitmap(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte R = (byte)rgbMatrix[x, y].Get(0);
                    byte G = (byte)rgbMatrix[x, y].Get(1);
                    byte B = (byte)rgbMatrix[x, y].Get(2);
                    Color pixel = Color.FromArgb(255, R, G, B);
                    bitmap.SetPixel(x, y, pixel);
                }
            }
            Graphics g = Graphics.FromImage(bitmap);
            g.Flush();
            bitmap.Save(path + $"\\{filename}.{format}");
            g.Dispose();
        }

        public static void InitializeMatrix(MatrixBitmap matrix)
        {
            int width = matrix.Columns;
            int height = matrix.Rows;
            int comp = matrix.ComponentsPerPixel;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    matrix[x, y] = new PixelData(comp);
                }
            }
        }
    }
}
