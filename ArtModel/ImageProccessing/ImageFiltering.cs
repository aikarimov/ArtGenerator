using ArtModel.MathLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace ArtModel.ImageProccessing
{
    public class ImageFiltering
    {

        public static Matrix2D<(double, double, double)> BitmapToMatrix(BitmapSource source)
        {
            FormatConvertedBitmap imageInRGB24 = new FormatConvertedBitmap(source, PixelFormats.Rgb24, BitmapPalettes.Halftone256, 0);

            int width = imageInRGB24.PixelWidth;
            int height = imageInRGB24.PixelHeight;
            int bytesPerPixel = (imageInRGB24.Format.BitsPerPixel + 7) / 8;
            int stride = width * bytesPerPixel;
            int size = height * stride;

            byte[] pixels = new byte[size];
            imageInRGB24.CopyPixels(pixels, stride, 0);

            Matrix2D<(double, double, double)> result = new Matrix2D<(double, double, double)>(height, width);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + bytesPerPixel * x;
                    result[y, x] = (pixels[index], pixels[index + 1], pixels[index + 2]);
                }
            }

            return result;
        }

        public static Matrix2D<double> ToGrayScale(Matrix2D<(double, double, double)> matrix)
        {
            int rows = matrix.Rows;
            int cols = matrix.Columns;

            Matrix2D<double> grayMatrix = new Matrix2D<double>(rows, cols);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    grayMatrix[y, x] = (byte)Math.Clamp(0.2126 * matrix[y, x].Item1 + 0.7152 * matrix[y, x].Item2 + 0.0722 * matrix[y, x].Item3, 0, 255);
                }
            }
            return grayMatrix;
        }

        public static WriteableBitmap MatrixToBitmap(Matrix2D<double> matrix)
        {
            int width = matrix.Columns;
            int height = matrix.Rows;
            int bytesPerPixel = (PixelFormats.Rgb24.BitsPerPixel + 7) / 8;
            int stride = width * bytesPerPixel;
            int size = height * stride;
            byte[] pixels = new byte[size];

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    byte value = (byte)matrix[row, col];

                    int index = (row * width + col) * 3;
                    pixels[index] = value;
                    pixels[index + 1] = value;
                    pixels[index + 2] = value;
                    //pixels[index + 3] = 255;
                }
            }

            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            bitmap.Lock();
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 3, 0);
            bitmap.Unlock();

            return bitmap;
        }

        public static WriteableBitmap MatrixToBitmap(Matrix2D<(double, double, double)> matrix)
        {
            int width = matrix.Columns;
            int height = matrix.Rows;
            int bytesPerPixel = (PixelFormats.Rgb24.BitsPerPixel + 7) / 8;
            int stride = width * bytesPerPixel;
            int size = height * stride;
            byte[] pixels = new byte[size];

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    int index = (row * width + col) * 3;
                    pixels[index] = (byte)matrix[row, col].Item1;
                    pixels[index + 1] = (byte)matrix[row, col].Item2;
                    pixels[index + 2] = (byte)matrix[row, col].Item3;
                }
            }

            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            bitmap.Lock();
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 3, 0);
            bitmap.Unlock();

            return bitmap;
        }

        public static Matrix2D<double> ApplySobel(Matrix2D<double> matrix)
        {
            Matrix2D<double> result = new Matrix2D<double>(matrix.Rows, matrix.Columns);

            double p1 = 0.183;

            double[,] sobelX = {
                { p1,         0,        -p1 },
                { 1 - 2 * p1, 0, 2 * p1 - 1 },
                { p1,         0,        -p1 }
            };

            double[,] sobelY = {
                { p1, 1 - 2 * p1, p1 },
                { 0, 0, 0 },
                { -p1, 2 * p1 - 1, -p1}
            };

            /* double[,] sobelX = {
                 { -0.5 * p1, 0, 0.5 * p1 },
                 {  p1 - 0.5, 0, 0.5 - p1 },
                 { -0.5 * p1, 0, 0.5 * p1 }
             };

             double[,] sobelY = {
                 { -0.5 * p1, p1 - 0.5, -0.5 * p1 },
                 {         0,        0,         0 },
                 {  0.5 * p1, 0.5 - p1,  0.5 * p1 }
             };*/

            /*            double[,] sobelX = {
                        { -1, 0, 1 },
                        { -2, 0, 2 },
                        { -1, 0, 1 }};

                        double[,] sobelY = {
                        { -1, -2, -1 },
                        { 0, 0, 0 },
                        { 1, 2, 1 }};*/

            var dx = ApplyConvolution(matrix, sobelX);
            var dy = ApplyConvolution(matrix, sobelY);

            for (int i = 0; i < matrix.Rows; i++)
            {
                for (int j = 0; j < matrix.Columns; j++)
                {
                    var pixelX = dx[i, j];
                    var pixelY = dy[i, j];

                    double value = Math.Sqrt(pixelX * pixelX + pixelY * pixelY);
                    result[i, j] = (byte)Math.Clamp(value, 0, 255);

                    /* double angle = Math.Atan2(pixelY, pixelX);

                     result[i, j] = (byte)Math.Clamp((angle + Math.PI / 2) / (Math.PI) * 255, 0, 255);*/
                }
            }

            return result;
        }

        public static Matrix2D<double> ApplyConvolution(Matrix2D<double> matrix, double[,] kernel)
        {
            Matrix2D<double> result = new Matrix2D<double>(matrix.Rows, matrix.Columns);

            int rows = matrix.Rows;
            int columns = matrix.Columns;

            int kernelRows = kernel.GetLength(0);
            int kernelColumns = kernel.GetLength(1);

            int halfKernelRows = kernelRows / 2;
            int halfKernelColumns = kernelColumns / 2;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    double sum = 0.0;

                    for (int m = -halfKernelRows; m <= halfKernelRows; m++)
                    {
                        for (int n = -halfKernelColumns; n <= halfKernelColumns; n++)
                        {
                            int rowIndex = i + m;
                            int colIndex = j + n;

                            if (rowIndex >= 0 && rowIndex < rows && colIndex >= 0 && colIndex < columns)
                            {
                                sum += matrix[rowIndex, colIndex] * kernel[m + halfKernelRows, n + halfKernelColumns];
                            }
                        }
                    }

                    result[i, j] = sum;
                }
            }

            return result;
        }
    }
}
