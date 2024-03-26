using ArtModel.Core;
using ArtModel.ImageProccessing;
using ArtModel.MathLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ArtModel.Tracing
{
    public class SquareTile
    {
        public int SquareSize { get; set; }
        public int SquareStep { get; set; }

        [NonSerialized()]
        public static readonly SquareTile SquareDefault = new SquareTile(15, 15);

        public SquareTile(int squareSize, int squareStep)
        {
            SquareSize = squareSize;
            SquareStep = squareStep;
        }
    }

    public class TracerPointDecider
    {
        private ArtBitmap _original;

        private ArtBitmap _artificial;

        private Random _random;

        public RandomPoolGenerator Pool;

        public TracerPointDecider(ArtBitmap original, ArtBitmap arificial, int randomSeed = -1)
        {
            _original = original;
            _artificial = arificial;
            _random = randomSeed == -1 ? new Random() : new Random(randomSeed);
            Pool = new RandomPoolGenerator(original.Width, original.Height, randomSeed);
        }

        public (int x, int y) GetMaxDispersionPoint()
        {
            (int x, int y) pixel = (0, 0);
            double maxDisp = 0;
            for (int x = 0; x < _original.Width; x++)
            {
                for (int y = 0; y < _original.Height; y++)
                {
                    double d = ColorEuclideanDistance(_original[x, y], _artificial[x, y]);
                    if (d > maxDisp)
                    {
                        maxDisp = d;
                        pixel = (x, y);
                    }
                }
            }
            Pool.RemoveFromPool(pixel);
            return pixel;
        }


        public (int x, int y) GetRandomPointPool()
        {
            return Pool.GetFromPoolRandom();
        }

        public bool PoolAvaliable()
        {
            return Pool.PoolAvaliable();
        }

        // Выбор точки из числа N с наибольшей дисперсией в области
        public CircleTracingResult GetStartingPoint(ArtGeneration genData, RandomPoolGenerator pool)
        {
            int attempts = 5;

            (int x, int y)[] coordinatesPool = new (int x, int y)[attempts];
            for (int i = 0; i < attempts; i++)
            {
                coordinatesPool[i] = pool.GetFromPoolRandom();
            }

            Task[] tasks = new Task[attempts];
            CircleTracingResult[] results = new CircleTracingResult[attempts];

            for (int i = 0; i < attempts; i++)
            {
                int index = i;
                tasks[index] = Task.Run(() =>
                {
                    results[index] = CircleTracer.TraceIterative(genData, _original, coordinatesPool[index]);
                });
            }

            Task.WaitAll(tasks);

            var max = results.Max(x => x.Dispersion);
            foreach (var res in results)
            {
                if (res.Dispersion == max)
                {
                    return res;
                }
            }
            return results[0];
        }

        // Взвешенный выбор точки на основе дисперсии цветов
        public (int x, int y) PreciseSamplingWeighted(SquareTile tile)
        {
            Dictionary<(int x, int y), double> errorCache = new();

            List<Rectangle> squares = GetSquares(tile);

            List<double> weights = new List<double>();
            foreach (var sq in squares)
            {
                double weight = CalculateSquareError(sq, errorCache);
                weights.Add(weight);
            }

            int squareIndex = 0;
            double totalWeight = weights.Sum();
            double randomValue = _random.NextDouble() * totalWeight;

            for (int i = 0; i < weights.Count; i++)
            {
                randomValue -= weights[i];
                if (randomValue <= 0)
                {
                    squareIndex = i;
                    break;
                }
            }

            return GetPointsWithMaxErrorInSquare(squares[squareIndex], errorCache, 1)[0];
        }

        public HashSet<(int x, int y)> PreciseSampling(ArtBitmap original, ArtBitmap artificial, SquareTile tile)
        {
            HashSet<(int x, int y)> maxErrorPoints = new HashSet<(int x, int y)>();

            /* Dictionary<(int x, int y), double> errorCache = new Dictionary<(int x, int y), double>();

             List<Rectangle> squares = GetSquares(tile, original.Width, original.Height);

             squares.Sort((s1, s2) => CalculateSquareError(s2, errorCache).CompareTo(CalculateSquareError(s1, errorCache)));

             Parallel.ForEach(squares.Take(1), square =>
             {
                 List<(int x, int y)> squareMaxErrorPoints = GetPointsWithMaxErrorInSquare(square);

                 lock (maxErrorPoints)
                 {
                     foreach (var point in squareMaxErrorPoints)
                     {
                         maxErrorPoints.Add(point);
                     }
                 }
             });*/

            return maxErrorPoints;
        }

        private List<(int x, int y)> GetPointsWithMaxErrorInSquare(Rectangle square, Dictionary<(int x, int y), double> errorCache, int pointsNum)
        {
            List<(int x, int y)> allPointsInSquare = new List<(int x, int y)>();
            for (int x = square.X; x < square.Right; x++)
            {
                for (int y = square.Y; y < square.Bottom; y++)
                {
                    allPointsInSquare.Add((x, y));
                }
            }
            allPointsInSquare.Sort((p1, p2) => errorCache[p2].CompareTo(errorCache[p1]));
            List<(int x, int y)> maxErrorPoints = [.. allPointsInSquare.Take(pointsNum)];
            return maxErrorPoints;
        }

        private double ColorEuclideanDistance(in Color color1, in Color color2)
        {
            double redDiff = color1.R - color2.R;
            double greenDiff = color1.G - color2.G;
            double blueDiff = color1.B - color2.B;
            return Math.Sqrt(redDiff * redDiff + greenDiff * greenDiff + blueDiff * blueDiff);
        }

        private double CalculateSquareError(in Rectangle square, Dictionary<(int x, int y), double> errorCache)
        {
            double totalError = 0;

            for (int x = square.X; x < square.Right; x++)
            {
                for (int y = square.Y; y < square.Bottom; y++)
                {
                    (int x, int y) pixel = (x, y);

                    if (errorCache.TryGetValue(pixel, out double pixelError))
                    {
                        totalError += pixelError;
                    }
                    else
                    {
                        Color originalColor = _original[x, y];
                        Color artificialColor = _artificial[x, y];
                        double pixelColorDifference = ColorEuclideanDistance(originalColor, artificialColor);
                        errorCache[pixel] = pixelColorDifference;
                        totalError += pixelColorDifference;
                    }
                }
            }

            return totalError;
        }

        private List<Rectangle> GetSquares(SquareTile tile)
        {
            int size = tile.SquareSize;
            int step = tile.SquareStep;
            List<Rectangle> list = new List<Rectangle>();

            for (int x = 0; x < _original.Width; x += step)
            {
                for (int y = 0; y < _original.Height; y += step)
                {
                    list.Add(new Rectangle(x, y,
                        Math.Min(size, _original.Width - 1 - x),
                        Math.Min(size, _original.Height - 1 - y)));
                }
            }

            return list;
        }
    }
}
