using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ArtModel.ImageModel.Tracing.GenerationData;

namespace ArtModel.ImageModel.Tracing
{
    public struct CircleTracingResult
    {
        public CircleTracingResult()
        {

        }

        public HashSet<(int x, int y)> Coordinates;
        public MeanColorCalculator Calculator;
        public int Radius;
        public double Dispersion;
    }

    public class CircleTracer
    {
        public static CircleTracingResult TraceIterative(SingleGenerationData genData, ArtBitmap bitmap, (int x, int y) point)
        {
            int x = point.x;
            int y = point.y;
            int r_min = genData.StrokeWidth.min;
            int r_max = genData.StrokeWidth.max;
            double tolerance = genData.Dispersion;

            Task[] tasks = new Task[r_max - r_min + 1];
            CircleTracingResult[] rois = new CircleTracingResult[r_max - r_min + 1];

            for (int radius = r_min; radius <= r_max; radius++)
            {
                int index = radius - r_min;
                int rad = radius;

                tasks[index] = Task.Run(() =>
                {
                    CircleMaskResult circle = StrokeCircleMask.ApplyCircleMask(bitmap, x, y, rad);
                    MeanColorCalculator calc = new MeanColorCalculator(bitmap, circle.Coordinates);
                    double dispesion = StrokeUtils.GetDispersion(bitmap, circle.Coordinates, calc.GetMeanColor());

                    rois[index] = new CircleTracingResult()
                    {
                        Coordinates = circle.Coordinates,
                        Calculator = calc,
                        Radius = rad,
                        Dispersion = dispesion
                    };
                });
            }

            Task.WaitAll(tasks);

            for (int i = rois.Length - 1; i >= 0; i--)
            {
                CircleTracingResult ro = rois[i];
                if (ro.Dispersion <= tolerance)
                {
                    return ro;
                }
            }

            return rois[0];
        }
    }

    public struct CircleMaskResult
    {
        public CircleMaskResult(HashSet<(int x, int y)> coordinates)
        {
            Coordinates = coordinates;
        }

        public HashSet<(int x, int y)> Coordinates { get; private set; }
    }

    public static class StrokeCircleMask
    {
        private static Dictionary<int, bool[,]> _masks = new();

        static StrokeCircleMask()
        {
            for (int i = 0; i < 15; i++)
            {
                GetMask(i);
            }
        }

        public static CircleMaskResult ApplyCircleMask(ArtBitmap bitmap, int x, int y, int radius)
        {
            bool[,] mask = GetMask(radius);

            HashSet<(int x, int y)> coordinates = new();

            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    if (mask[radius + j, radius + i])
                    {
                        int rx = x + i;
                        int ry = y + j;

                        if (rx >= 0 && rx < bitmap.Width && ry >= 0 && ry < bitmap.Height)
                        {
                            coordinates.Add((rx, ry));
                        }
                    }
                }
            }

            return new CircleMaskResult(coordinates);
        }

        private static bool[,] GetMask(int radius)
        {
            if (_masks.ContainsKey(radius))
            {
                return _masks[radius];
            }
            else
            {
                var mask = CreateNewCircleMask(radius);
                _masks.Add(radius, mask);
                return mask;
            }
        }

        private static bool[,] CreateNewCircleMask(int radius)
        {
            int diameter = 2 * radius + 1;
            bool[,] mask = new bool[diameter, diameter];

            int centerX = radius;
            int centerY = radius;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    int distanceSquared = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);
                    mask[y, x] = distanceSquared <= radius * radius;
                }
            }

            return mask;
        }
    }
}
