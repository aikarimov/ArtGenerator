using ArtModel.Image.Matrix;
using System.Drawing.Drawing2D;

namespace ArtModel.ImageProccessing
{
    public class GaussianBlur
    {
        private enum Direction
        {
            Horizontal,
            Vertical
        }

        public static MatrixBitmap ApplyGaussianBlurToRGB(MatrixBitmap matrixImg, double sigma)
        {
            if (sigma == 0)
            {
                return matrixImg;
            }

            int kernelSize = (int)Math.Ceiling(6 * sigma);
            if (kernelSize % 2 == 0)
            {
                kernelSize++;
            }

            double[,] kernelX = Generate1dGaussianKernel(kernelSize, sigma, Direction.Horizontal);
            double[,] kernelY = Generate1dGaussianKernel(kernelSize, sigma, Direction.Vertical);

            MatrixBitmap blurX = ApplyKernel(matrixImg, kernelX);
            MatrixBitmap blurXY = ApplyKernel(blurX, kernelY);

            return blurXY;
        }

        static MatrixBitmap ApplyKernel(MatrixBitmap matrix, double[,] kernel)
        {
            return ImageFiltering.ApplyConvolution(matrix, kernel);
        }

        private static double[,] Generate1dGaussianKernel(int kernelSize, double sigma, Direction direction)
        {
            int halfSize = kernelSize / 2;

            double[,] kernel;

            if (direction == Direction.Horizontal)
            {
                kernel = new double[1, kernelSize];
                for (int i = -halfSize; i <= halfSize; i++)
                {
                    kernel[0, i + halfSize] = G(i, sigma);
                }
            }
            else
            {
                kernel = new double[kernelSize, 1];
                for (int i = -halfSize; i <= halfSize; i++)
                {
                    kernel[i + halfSize, 0] = G(i, sigma);
                }
            }

            return kernel;
        }

        private static double G(int x, double sigma)
        {
            return 1.0f / Math.Sqrt(2 * Math.PI * sigma * sigma) * Math.Exp(-(x * x) / (2 * sigma * sigma));
        }
    }
}
