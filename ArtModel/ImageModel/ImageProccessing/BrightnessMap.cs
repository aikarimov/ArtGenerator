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
                 { -0.5 * p1, 0, 0.5 * p1 },
                 {  p1 - 0.5, 0, 0.5 - p1 },
                 { -0.5 * p1, 0, 0.5 * p1 }};
        private static double[,] sobelY3 = {
                 { -0.5 * p1, p1 - 0.5, -0.5 * p1 },
                 {         0,        0,         0 },
                 {  0.5 * p1, 0.5 - p1,  0.5 * p1 }};

        public static double[,] GetBrightnessMap(ArtBitmap bitmap)
        {
            byte[,] gray = ImageFiltering.ToGrayScale(bitmap);

            double[,] dx = ImageFiltering.ApplyConvolution(gray, sobelX2);
            double[,] dy = ImageFiltering.ApplyConvolution(gray, sobelY2);

            double[,] result = new double[bitmap.Height, bitmap.Width];

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    result[y, x] = Math.Atan2(dy[y, x], dx[y, x]);
                }
            }

            return result;
        }
    }
}
