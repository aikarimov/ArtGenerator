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
                 { -0.5 * p1, 0, 0.5 * p1 },
                 {  p1 - 0.5, 0, 0.5 - p1 },
                 { -0.5 * p1, 0, 0.5 * p1 }};
        private static double[,] sobelY3 = {
                 { -0.5 * p1, p1 - 0.5, -0.5 * p1 },
                 {         0,        0,         0 },
                 {  0.5 * p1, 0.5 - p1,  0.5 * p1 }};

        public static double[,] GetBrightnessMap(ArtBitmap origBm, double blurSigma)
        {
            byte[,] gray = ImageFiltering.ToGrayScale(origBm);

            double[,] dx = ImageFiltering.ApplyConvolution(gray, sobelX2);
            double[,] dy = ImageFiltering.ApplyConvolution(gray, sobelY2);

            double[,] result = new double[origBm.Height, origBm.Width];
            ArtBitmap resMap = new ArtBitmap(origBm.Width, origBm.Height);
            ArtBitmap edgeMap = new ArtBitmap(origBm.Width, origBm.Height);

            for (int x = 0; x < origBm.Width; x++)
            {
                for (int y = 0; y < origBm.Height; y++)
                {
                    double angle = Math.Atan2(dy[y, x], dx[y, x]);
                    result[y, x] = angle;
                }
            }

            result = GaussianBlur.ApplyBlur(result, blurSigma);

            for (int x = 0; x < origBm.Width; x++)
            {
                for (int y = 0; y < origBm.Height; y++)
                {
                    double angle = result[y, x];
                    byte grayAngle = (byte)Math.Clamp(((angle + Math.PI) / (2 * Math.PI)) * 255, 0, 255);
                    resMap[x, y] = Color.FromArgb(grayAngle, grayAngle, grayAngle);
                }
            }


            //resMap.Save("C:\\Users\\skura\\source\\repos\\ArtGenerator\\ArtModel\\Output", "Grad");
            //edgeMap.Save("C:\\Users\\skura\\source\\repos\\ArtGenerator\\ArtModel\\Output", "edgeMap");
            resMap.Save("C:\\Users\\skura\\source\\repos\\ArtGenerator\\ArtModel\\Output", $"GradBlurred{blurSigma}");
            return result;
        }
    }
}
