using ArtModel.ImageProccessing;

namespace ArtModel.ImageModel.ImageProccessing
{
    public class BrightnessMap
    {
        private const double p1 = 0.183;

        // Ядра чувака из старого диплома. (откуда они??)
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

        // Просто ядра Собеля
        private static double[,] sobelX2 = {
                        { -1, 0, 1 },
                        { -2, 0, 2 },
                        { -1, 0, 1 }};
        private static double[,] sobelY2 = {
                        { 1, 2, 1 },
                        { 0, 0, 0 },
                        { -1, -2, -1 }};

        // Крутые ядра из научной статьи
        private static double[,] sobelX3 = {
                 { -0.5 * p1, 0, 0.5 * p1 },
                 {  p1 - 0.5, 0, 0.5 - p1 },
                 { -0.5 * p1, 0, 0.5 * p1 }};
        private static double[,] sobelY3 = {
                 { -0.5 * p1, p1 - 0.5, -0.5 * p1 },
                 {         0,        0,         0 },
                 {  0.5 * p1, 0.5 - p1,  0.5 * p1 }};

        public static double[,] GetBrightnessMap(ArtBitmap origBm)
        {
            byte[,] gray = ImageFiltering.ToGrayScale(origBm);

            double[,] dx = ImageFiltering.ApplyConvolution(gray, sobelX2);
            double[,] dy = ImageFiltering.ApplyConvolution(gray, sobelY2);

            double[,] result = new double[origBm.Height, origBm.Width];

            for (int x = 0; x < origBm.Width; x++)
            {
                for (int y = 0; y < origBm.Height; y++)
                {
                    double angle = Math.Atan2(dy[y, x], dx[y, x]);
                    result[y, x] = /*Math.Round(angle, 5)*/ angle;
                }
            }

            return result;
        }
    }
}
