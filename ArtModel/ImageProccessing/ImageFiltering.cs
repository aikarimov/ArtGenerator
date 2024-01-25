using ArtModel.ColorModel;
using ArtModel.Image.Matrix;
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
            for (int i = halfKernelRows; i < rows - halfKernelRows; i++)
            {
                for (int j = halfKernelColumns; j < columns - halfKernelColumns; j++)
                {
                    double sum = 0.0f;

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

        public static MatrixBitmap ApplyConvolution(MatrixBitmap matrix, double[,] kernel)
        {
            MatrixBitmap result = new MatrixBitmap(matrix);

            int height = matrix.Height;
            int width = matrix.Width;

            int kernelHeight = kernel.GetLength(0);
            int kernelWidth = kernel.GetLength(1);

            int halfKernelHeight = kernelHeight / 2;
            int halfKernelWidth = kernelWidth / 2;

            int components = matrix.PixelFormat;

            // Обход главной матрицы
            for (int y = 0; y < height; y++)
            {

                for (int x = 0; x < width; x++)
                {
                    double[] sum = new double[components];

                    // Обход ядра
                    for (int m = -halfKernelHeight; m <= halfKernelHeight; m++)
                    {
                        for (int n = -halfKernelWidth; n <= halfKernelWidth; n++)
                        {
                            int k_y = Math.Clamp(y + m, halfKernelHeight, height - halfKernelHeight - 1);
                            int k_x = Math.Clamp(x + n, halfKernelWidth, width - halfKernelWidth - 1);

                            for (int c = 0; c < components; c++)
                            {
                                sum[c] += matrix[k_x, k_y][c] * kernel[m + halfKernelHeight, n + halfKernelWidth];
                            }
                        }
                    }

                    //result[x, y] = new PixelData(sum);
                }
            }

            return result;
        }
    }
}
