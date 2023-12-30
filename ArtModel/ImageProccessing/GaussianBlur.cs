using ArtModel.MathLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.ImageProccessing
{
    public class GaussianBlur
    {
        private enum Direction
        {
            X,
            Y
        }

        public static Matrix2D<(double, double, double)> ApplyGaussianBlur(Matrix2D<(double, double, double)> matrix, double sigma)
        {
            int rows = matrix.Rows;
            int cols = matrix.Columns;

            Matrix2D<double> matrix_Red = new Matrix2D<double>(matrix.Rows, matrix.Columns);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix_Red[i, j] = (matrix[i, j].Item1);
                }
            }

            Matrix2D<double> matrix_Green = new Matrix2D<double>(matrix.Rows, matrix.Columns);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix_Green[i, j] = (matrix[i, j].Item2);
                }
            }

            Matrix2D<double> matrix_Blue = new Matrix2D<double>(matrix.Rows, matrix.Columns);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix_Blue[i, j] = (matrix[i, j].Item3);
                }
            }

            var g1 = ApplyGaussianBlur(matrix_Red, sigma);
            var g2 = ApplyGaussianBlur(matrix_Green, sigma);
            var g3 = ApplyGaussianBlur(matrix_Blue, sigma);

            Matrix2D<(double, double, double)> result = new Matrix2D<(double, double, double)>(rows, cols);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = (g1[i, j], g2[i, j], g3[i, j]);
                }
            }

            return result;
        }


        public static Matrix2D<double> ApplyGaussianBlur(Matrix2D<double> matrix, double sigma)
        {
            if (sigma < 1)
            {
                return matrix;
            }

            int kernelSize = (int)Math.Ceiling(6 * sigma);
            if (kernelSize % 2 == 0)
            {
                kernelSize++;
            }

            var kernelX = GenerateGaussianKernel(kernelSize, sigma, Direction.X);
            var kernelY = GenerateGaussianKernel(kernelSize, sigma, Direction.Y);

            var blurX = ApplyKernel(matrix, kernelX);
            var blurXY = ApplyKernel(blurX, kernelY);

            return blurXY;
        }

        static Matrix2D<double> ApplyKernel(Matrix2D<double> matrix, double[,] kernel)
        {
            return ImageFiltering.ApplyConvolution(matrix, kernel);
        }

        private static double[,] GenerateGaussianKernel(int kernelSize, double sigma, Direction direction)
        {
            int halfSize = kernelSize / 2;

            double[,] kernel;

            if (direction == Direction.X)
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
