using System.Drawing;
using ArtModel.ImageProccessing;

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

        public static double[,] ApplyConvolution(double[,] core, double[,] kernel)
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

        public static ArtBitmap ApplyConvolution(ArtBitmap core, double[,] kernel)
        {
            int rows = core.Height;
            int cols = core.Width;

            ArtBitmap result = new ArtBitmap(cols, rows);

            int kernelRows = kernel.GetLength(0);
            int kernelColumns = kernel.GetLength(1);

            int halfKernelRows = kernelRows / 2;
            int halfKernelColumns = kernelColumns / 2;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    double sum_R = 0.0;
                    double sum_G = 0.0;
                    double sum_B = 0.0;

                    for (int m = -halfKernelRows; m <= halfKernelRows; m++)
                    {
                        for (int n = -halfKernelColumns; n <= halfKernelColumns; n++)
                        {
                            int k_y = Math.Clamp(y + m, halfKernelRows, rows - halfKernelRows - 1);
                            int k_x = Math.Clamp(x + n, halfKernelColumns, cols - halfKernelColumns - 1);

                            double kernel_value = kernel[m + halfKernelRows, n + halfKernelColumns];
                            Color color = core[k_x, k_y];

                            sum_R += color.R * kernel_value;
                            sum_G += color.G * kernel_value;
                            sum_B += color.B * kernel_value;
                        }
                    }

                    int R = (int)Math.Clamp(sum_R, 0, 255);
                    int G = (int)Math.Clamp(sum_G, 0, 255);
                    int B = (int)Math.Clamp(sum_B, 0, 255);


                    result[x, y] = Color.FromArgb(R, G, B);
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
