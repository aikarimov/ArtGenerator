using ArtModel.Image;

namespace ArtModel.ColorModel.ColorSpaces
{
    public abstract class ColorSpaceAbstract
    {
        public virtual int ComponentsCount { get; init; }

        public virtual double ToGrayScale(in PixelData pixel)
        {
            double sum = 0.0;
            for (int i = 0; i < ComponentsCount; i++)
            {
                sum += pixel.Get(i);
            }
            return (byte)Math.Clamp((sum) / ComponentsCount, 0, 255);
        }

        public virtual PixelData GetAveragePixelColor(in PixelData[] pixels)
        {
            double[] average = new double[ComponentsCount];

            foreach (var pixel in pixels)
            {
                for (int i = 0; i < ComponentsCount; i++)
                {
                    average[i] += pixel.Get(i);
                }
            }

            for (int i = 0; i < ComponentsCount; i++)
            {
                average[i] /= pixels.Length;
            }

            return new PixelData(average);
        }

        public virtual MatrixBitmap ToRGB(MatrixBitmap matrix)
        {
            return matrix;
        }

        public virtual MatrixBitmap FromRGB(MatrixBitmap matrix)
        {
            return matrix;
        }
    }
}
