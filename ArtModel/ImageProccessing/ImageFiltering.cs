using ArtModel.ColorModel.ColorSpaces.Spaces;
using ArtModel.MathLib;

namespace ArtModel.ImageProccessing
{
    public class ImageFiltering
    {
        public static Matrix2D<double> ApplyConvolution(Matrix2D<double> matrix, double[,] kernel)
        {
            Matrix2D<double> result = new Matrix2D<double>(matrix.Rows, matrix.Columns);

            int rows = matrix.Rows;
            int columns = matrix.Columns;

            int kernelRows = kernel.GetLength(0);
            int kernelColumns = kernel.GetLength(1);

            int halfKernelRows = kernelRows / 2;
            int halfKernelColumns = kernelColumns / 2;

            // Обход главной матрицы
            for (int i = halfKernelRows; i < (rows - halfKernelRows); i++)
            {
                for (int j = halfKernelColumns; j < (columns - halfKernelColumns); j++)
                {
                    double sum = 0.0;

                    // Обход ядра
                    for (int m = -halfKernelRows; m <= halfKernelRows; m++)
                    {
                        for (int n = -halfKernelColumns; n <= halfKernelColumns; n++)
                        {
                            int rowIndex = i + m;
                            int colIndex = j + n;

                            sum += matrix[rowIndex, colIndex] * kernel[m + halfKernelRows, n + halfKernelColumns];
                        }
                    }

                    result[i, j] = sum;
                }
            }

            return result;
        }
    }
}
