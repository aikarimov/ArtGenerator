using ArtModel.ImageProccessing;
using System.Drawing;

namespace ArtModel.ImageModel.ImageProccessing
{
    // Карта яркости - карта градиентов
    public class BrightnessMap
    {
        private const double p1 = 0.183;

        // Разные ядра свертки
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
                        { 1, 2, 1 },
                        { 0, 0, 0 },
                        { -1, -2, -1 }};


        private static double[,] sobelX3 = {
                        { -1, 0, 1 },
                        { -2, 0, 2 },
                        { -1, 0, 1 }};
        private static double[,] sobelY3 = {
                        { 1, 2, 1 },
                        { 0, 0, 0 },
                        { -1, -2, -1 }};

        public static double[,] GetBrightnessMap(ArtBitmap origBm, int brushRadius)
        {
            // Создание карты оттенков серого
            double[,] gray = ImageFiltering.ToGrayScale(origBm);

            // Создание градиентов
            double[,] dx = ImageFiltering.ApplyConvolution(gray, sobelX2);
            double[,] dy = ImageFiltering.ApplyConvolution(gray, sobelY2);

            // Применение фильтра сглаживания временно отключено
            var averagingFilter = GetAveragingFilter(brushRadius);
            //dx = ImageFiltering.ApplyConvolution(dx, averagingFilter);
            //dy = ImageFiltering.ApplyConvolution(dy, averagingFilter);

            double[,] result = new double[origBm.Height, origBm.Width];

            // Считает угол градиента для каждого пискля
            for (int x = 0; x < origBm.Width; x++)
            {
                for (int y = 0; y < origBm.Height; y++)
                {
                    result[y, x] = Math.Atan2(dy[y, x], dx[y, x]);
                }
            }
            return result;
        }

        // Создание усредняющего фильтра. Радиус кисти умножается на 4, и делается нечётным. Потому что свёртка сделана тголько для матриц с нечётным количеством рядом и колонок
        private static double[,] GetAveragingFilter(int brushRadius)
        {
            int m = 4 * brushRadius;
            if (m % 2 == 0)
                m++;

            double[,] filter = new double[m, m];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    filter[i, j] = 1.0 / (m * m);
                }
            }

            return filter;
        }
    }
}
