using ArtModel.ColorModel;
using ArtModel.Image.Matrix;
using ArtModel.MathLib;
using System.Drawing.Drawing2D;

namespace ArtModel.ImageProccessing
{
    public class BrightnessMap
    {
        private const double p1 = 0.183;

        private static double[,] sobelX = {
                { p1, 0, -p1 },
                { 1 - 2 * p1, 0, 2 * p1 - 1 },
                { p1, 0, -p1 }
        };

        private static double[,] sobelY = {
                { -p1, 2 * p1 - 1, -p1},
                { 0, 0, 0 },
                { p1, 1 - 2 * p1, p1 }
        };


        private static double[,] sobelX2 = {
                        { -1, 0, 1 },
                        { -2, 0, 2 },
                        { -1, 0, 1 }};
        private static double[,] sobelY2 = {
                        { -1, -2, -1 },
                        { 0, 0, 0 },
                        { 1, 2, 1 }};
        private static double[,] sobelX3 = {
                 { -0.5 * p1, 0, 0.5 * p1 },
                 {  p1 - 0.5, 0, 0.5 - p1 },
                 { -0.5 * p1, 0, 0.5 * p1 }};
        private static double[,] sobelY3 = {
                 { -0.5 * p1, p1 - 0.5, -0.5 * p1 },
                 {         0,        0,         0 },
                 {  0.5 * p1, 0.5 - p1,  0.5 * p1 }};

        public static double[,] GetBrightnessMap(MatrixBitmap grayMatrix)
        {
            double[,] result = new double[grayMatrix.Height, grayMatrix.Width];

            var dx = ImageFiltering.ApplyConvolution(grayMatrix, sobelX);
            var dy = ImageFiltering.ApplyConvolution(grayMatrix, sobelY);

            for (int x = 0; x < grayMatrix.Width; x++)
            {
                for (int y = 0; y < grayMatrix.Height; y++)
                {
                    PixelData pixelX = dx[x, y];
                    PixelData pixelY = dy[x, y];

                    /*double value = Math.Sqrt(pixelX[0] * pixelX[0] + pixelY[0] * pixelY[0]);
                    result[x, y] = new PixelData([(byte)Math.Clamp(value, 0, 255)]);*/

                    result[x, y] = Math.Atan2(pixelY[0], pixelX[0]);
                }
            }

            return result;
        }
    }
}
