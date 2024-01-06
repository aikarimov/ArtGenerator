using ArtModel.ColorModel.ColorSpaces.Spaces;
using ArtModel.Image;
using ArtModel.MathLib;

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
            int rows = matrixImg.Rows;
            int cols = matrixImg.Columns;

            Matrix2D<double> matrix_Red = new Matrix2D<double>(matrixImg.Rows, matrixImg.Columns);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    matrix_Red[y, x] = matrixImg[x, y].Get(0);
                }
            }

            Matrix2D<double> matrix_Green = new Matrix2D<double>(matrixImg.Rows, matrixImg.Columns);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    matrix_Green[y, x] = matrixImg[x, y].Get(1);
                }
            }

            Matrix2D<double> matrix_Blue = new Matrix2D<double>(matrixImg.Rows, matrixImg.Columns);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    matrix_Blue[y, x] = matrixImg[x, y].Get(2);
                }
            }
            var g1 = ApplyGaussianBlur(matrix_Red, sigma);
            var g2 = ApplyGaussianBlur(matrix_Green, sigma);
            var g3 = ApplyGaussianBlur(matrix_Blue, sigma);

            MatrixBitmap result = new MatrixBitmap(rows, cols, matrixImg.ColorSpace);
            MatrixConverter.InitializeMatrix(result);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    result[x, y].Set(0, g1[y, x]);
                    result[x, y].Set(1, g2[y, x]);
                    result[x, y].Set(2, g3[y, x]);
                }
            }

            return result;
        }


        private static Matrix2D<double> ApplyGaussianBlur(Matrix2D<double> matrix, double sigma)
        {
            int kernelSize = (int)Math.Ceiling(6 * sigma);
            if (kernelSize % 2 == 0)
            {
                kernelSize++;
            }

            var kernelX = Generate1dGaussianKernel(kernelSize, sigma, Direction.Horizontal);
            var kernelY = Generate1dGaussianKernel(kernelSize, sigma, Direction.Vertical);

            var blurX = ApplyKernel(matrix, kernelX);
            var blurXY = ApplyKernel(blurX, kernelY);

            return blurXY;
        }

        static Matrix2D<double> ApplyKernel(Matrix2D<double> matrix, double[,] kernel)
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
            return 1 / (Math.Sqrt(2 * Math.PI * sigma * sigma)) * Math.Exp(-(x * x) / (2 * sigma * sigma));
        }
    }
}
