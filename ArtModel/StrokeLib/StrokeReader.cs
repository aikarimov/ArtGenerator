using System.Drawing;

namespace ArtModel.StrokeLib
{
    public class StrokeReader
    {
        public static Bitmap ReadStrokeCropped(Bitmap original)
        {
            ConvertToGrayScale(ref original);
            return CropImage(original, GetContentRectangle(original));
        }

        private static void ConvertToGrayScale(ref Bitmap original)
        {
            for (int x = 0; x < original.Width; x++)
            {
                for (int y = 0; y < original.Height; y++)
                {
                    Color pixelColor = original.GetPixel(x, y);
                    int grayValue = (int)(pixelColor.R * 0.3 + pixelColor.G * 0.59 + pixelColor.B * 0.11);
                    Color grayColor = Color.FromArgb(grayValue, grayValue, grayValue);
                    original.SetPixel(x, y, grayColor);
                }
            }
        }

        private static Rectangle GetContentRectangle(Bitmap image)
        {
            int left = 0, top = 0, right = image.Width - 1, bottom = image.Height - 1;
            int blackBorder = 240;

            // Находим левый край
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color col = image.GetPixel(x, y);
                    if (col.R < blackBorder)
                    {
                        left = x;
                        break;
                    }
                }
                if (left != 0)
                    break;
            }

            // Находим верхний край
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = left; x < image.Width; x++)
                {
                    if (image.GetPixel(x, y).R < blackBorder)
                    {
                        top = y;
                        break;
                    }
                }
                if (top != 0)
                    break;
            }

            // Находим правый край
            for (int x = image.Width - 1; x >= left; x--)
            {
                for (int y = top; y < image.Height; y++)
                {
                    if (image.GetPixel(x, y).R < blackBorder)
                    {
                        right = x;
                        break;
                    }
                }
                if (right != image.Width - 1)
                    break;
            }

            // Находим нижний край
            for (int y = image.Height - 1; y >= top; y--)
            {
                for (int x = left; x <= right; x++)
                {
                    if (image.GetPixel(x, y).R < blackBorder)
                    {
                        bottom = y;
                        break;
                    }
                }
                if (bottom != image.Height - 1)
                    break;
            }

            return new Rectangle(left, top, right - left + 1, bottom - top + 1);
        }

        private static Bitmap CropImage(Bitmap image, in Rectangle cropRectangle)
        {
            Bitmap croppedImage = new Bitmap(cropRectangle.Width, cropRectangle.Height);

            for (int x = 0; x < cropRectangle.Width; x++)
            {
                for (int y = 0; y < cropRectangle.Height; y++)
                {
                    Color pixelColor = image.GetPixel(cropRectangle.Left + x, cropRectangle.Top + y);
                    croppedImage.SetPixel(x, y, pixelColor);
                }
            }

            return croppedImage;
        }
    }
}
