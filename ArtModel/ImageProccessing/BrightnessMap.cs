using ArtModel.ImageProccessing;
using System.Drawing;

namespace ArtModel.ImageModel.ImageProccessing
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
            ArtBitmap artBitmapEdge = new ArtBitmap(origBm.Width, origBm.Height);
            ArtBitmap artBitmapGray = new ArtBitmap(origBm.Width, origBm.Height);

            double[,] gray = ImageFiltering.ToGrayScale(origBm);

            for (int x = 0; x < origBm.Width; x++)
            {
                for (int y = 0; y < origBm.Height; y++)
                {
                    int grayPixel = (int)Math.Round(gray[y, x]);

                    artBitmapGray[x, y] = Color.FromArgb(255, grayPixel, grayPixel, grayPixel);
                }
            }

            //artBitmapGray.Save("C:\\Users\\skura\\source\\repos\\ArtGenerator\\Output\\Images", "GrayImage");

            double[,] dx = ImageFiltering.ApplyConvolution(gray, sobelX);
            double[,] dy = ImageFiltering.ApplyConvolution(gray, sobelY);

            double[,] edges = new double[origBm.Height, origBm.Width];
            var averagingFilter = GetAveragingFilter(brushRadius);
            dx = ImageFiltering.ApplyConvolution(dx, averagingFilter);
            dy = ImageFiltering.ApplyConvolution(dy, averagingFilter);

            double[,] result = new double[origBm.Height, origBm.Width];

            for (int x = 0; x < origBm.Width; x++)
            {
                for (int y = 0; y < origBm.Height; y++)
                {
                    edges[y, x] = Math.Sqrt(Math.Pow(dy[y, x], 2) + Math.Pow(dx[y, x], 2));
                    result[y, x] = Math.Atan2(dy[y, x], dx[y, x]);
                }
            }

            double edge_max = double.MinValue;
            double edge_min = double.MaxValue;


            for (int x = 0; x < origBm.Width; x++)
            {
                for (int y = 0; y < origBm.Height; y++)
                {

                    var edge = edges[y, x];
                    if (edge > edge_max)
                        edge_max = edge;

                    if (edge < edge_min)
                        edge_min = edge;
                }
            }

            for (int x = 0; x < origBm.Width; x++)
            {
                for (int y = 0; y < origBm.Height; y++)
                {
                    double edge = edges[y, x];
                    double af3 = (edge - edge_min) / (edge_max - edge_min);
                    int col3 = (int)(Math.Round(af3 * 255.0));
                    artBitmapEdge[x, y] = Color.FromArgb(255, col3, col3, col3);
                }
            }

            //artBitmapEdge.Save("C:\\Users\\skura\\source\\repos\\ArtGenerator\\Output\\Images", "artBitmapEdge");

            return result;
        }

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
