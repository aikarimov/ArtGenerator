using ArtModel.Image;
using ArtModel.MathLib;

namespace ArtModel.ImageProccessing
{
    public class BrightnessMap
    {
        private const double p1 = 0.183;

        private static double[,] sobelX = {
                { p1, 0, -p1 },
                { 1 - 2 * p1, 0, 2 * p1 - 1 },
                { p1, 0, -p1 }};

        private static double[,] sobelY = {
                { p1, 1 - 2 * p1, p1 },
                { 0, 0, 0 },
                { -p1, 2 * p1 - 1, -p1}};


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

        public static Matrix2D<double> GetBrightnessMap(Matrix2D<double> inputGray)
        {
            Matrix2D<double> result = new Matrix2D<double>(inputGray.Rows, inputGray.Columns);

            var dx = ImageFiltering.ApplyConvolution(inputGray, sobelX);
            var dy = ImageFiltering.ApplyConvolution(inputGray, sobelY);

            for (int x = 0; x < inputGray.Rows; x++)
            {
                for (int y = 0; y < inputGray.Columns; y++)
                {
                    var pixelX = dx[x, y];
                    var pixelY = dy[x, y];

                    //double value = Math.Sqrt(pixelX * pixelX + pixelY * pixelY);
                    //result[i, j] = (byte)Math.Clamp(value, 0, 255);

                    double angle = Math.Atan2(pixelY, pixelX);
                    result[x, y] = (byte)Math.Clamp((angle + Math.PI) / (2 * Math.PI) * 255, 0, 255);
                }
            }

            return result;
        }
    }
}
