using ArtModel.ColorModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.Image.StrokeModel
{
    public class StrokeUtils
    {
        public static PixelData GetAverageColor(List<PixelData> pixels)
        {
            int components = PixelData.Components;

            int[] average = new int[components];

            foreach (var pixel in pixels)
            {
                for (int i = 0; i < components; i++)
                {
                    average[i] += pixel[i];
                }
            }

            for (byte i = 0; i < components; i++)
            {
                average[i] = (average[i] / pixels.Count());
            }

            PixelData pixelData = new PixelData();
            for (byte i = 0; i < components; i++)
            {
                pixelData[i] = (byte)(average[i]);
            }

            return pixelData;
        }

        public static (double dispersion, PixelData average) GetDispersion(List<PixelData> pixels)
        {
            PixelData average = GetAverageColor(pixels);

            double sum = 0.0;
            foreach (var pixel in pixels)
            {
                double eucl = CalculateSquaredEuclideanDistance(pixel, average);
                sum += eucl;
            }

            return (Math.Sqrt(sum / (pixels.Count())), average);

            static double CalculateSquaredEuclideanDistance(PixelData data1, PixelData data2)
            {
                byte components = PixelData.Components;
                double sum = 0.0;
                for (byte i = 0; i < components; i++)
                {
                    double delta = data1[i] - data2[i];
                    sum += delta * delta;
                }
                return sum;
            }
        }
    }
}
