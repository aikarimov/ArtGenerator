using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArtModel.Core;
using ArtModel.ImageProccessing;

namespace ArtModel.Tracing
{
    public struct CircleTracingResult
    {
        public HashSet<(int x, int y)> Coordinates;

        public MeanColorCalculator Calculator;

        public int Width;

        public double Dispersion;
    }

    public class CircleTracer
    {
        public static CircleTracingResult TraceIterative(ArtGeneration genData, ArtBitmap bitmap, (int x, int y) point)
        {
            int x = point.x;
            int y = point.y;
            int r_min = genData.StrokeWidth_Min / 2;
            int r_max = genData.StrokeWidth_Max / 2;
            double dispersion = genData.DispersionBound;

            int r_interval = r_max - r_min + 1;
            Task[] tasks = new Task[r_interval];
            CircleTracingResult[] rois = new CircleTracingResult[r_interval];

            for (int radius = r_min; radius <= r_max; radius++)
            {
                int index = radius - r_min;
                int r_curr = radius;

                tasks[index] = Task.Run(() =>
                {
                    CircleMaskResult circle = StrokeCircleMask.ApplyCircleMask(bitmap, x, y, r_curr);
                    MeanColorCalculator calc = new MeanColorCalculator(bitmap, circle.Coordinates);
                    double dispesion = StrokeUtils.GetDispersion(bitmap, calc.GetMeanColor(), circle.Coordinates);

                    rois[index] = new CircleTracingResult()
                    {
                        Coordinates = circle.Coordinates,
                        Calculator = calc,
                        Width = r_curr * 2,
                        Dispersion = dispesion
                    };
                });
            }

            Task.WaitAll(tasks);

            for (int i = rois.Length - 1; i >= 0; i--)
            {
                CircleTracingResult ro = rois[i];
                if (ro.Dispersion <= dispersion)
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

                        if (bitmap.IsInside(rx, ry))
                        {
                            coordinates.Add((rx, ry));
                        }
                    }
                }
            }

            return new CircleMaskResult(coordinates);
        }

        static object locker = new();
        private static bool[,] GetMask(int radius)
        {
            if (_masks.ContainsKey(radius))
            {
                return _masks[radius];
            }
            else
            {
                // Может это фигня, я без понятия.
                lock (locker)
                {
                    var mask = CreateNewCircleMask(radius);
                    _masks.TryAdd(radius, mask);
                    return mask;
                }
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
