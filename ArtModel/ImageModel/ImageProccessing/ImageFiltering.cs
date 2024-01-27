using System.Drawing;

namespace ArtModel.ImageModel.ImageProccessing
{
    public class ImageFiltering
    {
        public static double[,] ApplyConvolution(byte[,] core, double[,] kernel)
        {
            int rows = core.GetLength(0);
            int cols = core.GetLength(1);

            double[,] result = new double[rows, cols];

            int kernelRows = kernel.GetLength(0);
            int kernelColumns = kernel.GetLength(1);

            int halfKernelRows = kernelRows / 2;
            int halfKernelColumns = kernelColumns / 2;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    double sum = 0.0;

                    for (int m = -halfKernelRows; m <= halfKernelRows; m++)
                    {
                        for (int n = -halfKernelColumns; n <= halfKernelColumns; n++)
                        {
                            int k_y = Math.Clamp(y + m, halfKernelRows, rows - halfKernelRows - 1);
                            int k_x = Math.Clamp(x + n, halfKernelColumns, cols - halfKernelColumns - 1);

                            sum += core[k_y, k_x] * kernel[m + halfKernelRows, n + halfKernelColumns];
                        }
                    }

                    result[y, x] = sum;
                }
            }

            return result;
        }

        public static ArtBitmap ApplyConvolution(ArtBitmap artBitmap, double[,] kernel)
        {
            int height = artBitmap.Height;
            int width = artBitmap.Width;

            ArtBitmap result = new ArtBitmap(width, height);

            int kernelHeight = kernel.GetLength(0);
            int kernelWidth = kernel.GetLength(1);

            int halfKernelHeight = kernelHeight / 2;
            int halfKernelWidth = kernelWidth / 2;

            // Обход главной матрицы
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //double[] sum = new double[components];

                    // Обход ядра
                    for (int m = -halfKernelHeight; m <= halfKernelHeight; m++)
                    {
                        for (int n = -halfKernelWidth; n <= halfKernelWidth; n++)
                        {
                            int k_y = Math.Clamp(y + m, halfKernelHeight, height - halfKernelHeight - 1);
                            int k_x = Math.Clamp(x + n, halfKernelWidth, width - halfKernelWidth - 1);

                            /*for (int c = 0; c < components; c++)
                            {
                                sum[c] += matrix[k_x, k_y][c] * kernel[m + halfKernelHeight, n + halfKernelWidth];
                            }*/
                        }
                    }

                    //result[x, y] = new PixelData(sum);
                }
            }

            return result;
        }

        public static byte[,] ToGrayScale(ArtBitmap artBitmap)
        {
            int height = artBitmap.Height;
            int width = artBitmap.Width;
            byte[,] result = new byte[height, width];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color col = artBitmap[x, y];
                    byte gray = (byte)Math.Clamp(0.2126 * col.R + 0.7152 * col.G + 0.0722 * col.B, 0, 255);
                    result[y, x] = gray;
                }
            }

            return result;
        }
    }
}
