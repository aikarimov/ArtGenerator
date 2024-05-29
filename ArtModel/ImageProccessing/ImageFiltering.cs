using System;
using System.Drawing;
using ArtModel.ImageProccessing;

namespace ArtModel.ImageModel.ImageProccessing
{
    public class ImageFiltering
    {
        public static double[,] ApplyConvolution(double[,] matrix, double[,] kernel)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double[,] result = new double[rows, cols];

            int kernelRows = kernel.GetLength(0);
            int kernelColumns = kernel.GetLength(1);

            int halfKernelRows = (kernelRows - 1) / 2;
            int halfKernelColumns = (kernelColumns - 1) / 2;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    double sum = 0.0;


                    for (int m = -halfKernelRows; m <= halfKernelRows; m++)
                    {
                        for (int n = -halfKernelColumns; n <= halfKernelColumns; n++)
                        {
                            int k_y = Math.Clamp(y + m, 0, rows - 1);
                            int k_x = Math.Clamp(x + n, 0, cols - 1);

                            sum += matrix[k_y, k_x] * kernel[m + halfKernelRows, n + halfKernelColumns];
                        }
                    }

                    result[y, x] = sum;
                }
            }

            return result;
        }

        public static double[,] ApplyConvolutionToAngles(double[,] matrix, double[,] kernel)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

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

                    double middleAngle = matrix[y, x];

                    for (int m = -halfKernelRows; m <= halfKernelRows; m++)
                    {
                        for (int n = -halfKernelColumns; n <= halfKernelColumns; n++)
                        {
                            int k_y = Math.Clamp(y + m, 0, rows - 1);
                            int k_x = Math.Clamp(x + n, 0, cols - 1);

                            double currentAngle = matrix[k_y, k_x];

                            //RecalcQuarter(ref currentAngle, middleAngle);

                            sum += currentAngle * kernel[m + halfKernelRows, n + halfKernelColumns];
                        }
                    }

                    //sum /= Math.Tau;

                    result[y, x] = sum;
                }
            }

            return result;

            void RecalcQuarter(ref double currentAngle, in double middleAngle)
            {
                switch (Math.Sign(middleAngle))
                {
                    // Исходный угол отрицательный
                    case -1:
                        if (currentAngle > 0)
                            currentAngle -= Math.Tau;
                        break;
                    // Исходный угол положительный
                    case 1:
                        if (currentAngle < 0)
                            currentAngle += Math.Tau;
                        break;
                }
            }
        }

        public static ArtBitmap ApplyConvolution(ArtBitmap matrix, double[,] kernel)
        {
            int rows = matrix.Height;
            int cols = matrix.Width;

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
                            int k_y = Math.Clamp(y + m, 0, rows - 1);
                            int k_x = Math.Clamp(x + n, 0, cols - 1);

                            double kernel_value = kernel[m + halfKernelRows, n + halfKernelColumns];
                            Color color = matrix[k_x, k_y];

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

        public static double[,] ToGrayScale(ArtBitmap artBitmap)
        {
            int height = artBitmap.Height;
            int width = artBitmap.Width;
            double[,] result = new double[height, width];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color col = artBitmap[x, y];
                    result[y, x] = Math.Clamp(0.2126 * col.R + 0.7152 * col.G + 0.0722 * col.B, 0, 255);
                }
            }

            return result;
        }
    }
}
