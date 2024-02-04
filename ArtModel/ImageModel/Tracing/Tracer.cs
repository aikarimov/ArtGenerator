using ArtModel.ImageModel.ImageProccessing;
using ArtModel.MathLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static ArtModel.ImageModel.Tracing.GenerationData;

namespace ArtModel.ImageModel.Tracing
{
    public struct TracingPath
    {
        public TracingPath()
        {

        }

        public MeanColorCalculator Calculator;
        public HashSet<(int x, int y)> Coordinates;
        public Color MeanColor;
        public double Dispersion;
    }

    public struct TracingResult
    {
        public TracingResult(HashSet<(int x, int y)> coordinates, Color meanColor)
        {
            Coordinates = coordinates;
            MeanColor = meanColor;
        }

        public HashSet<(int x, int y)> Coordinates;
        public Color MeanColor;
    }

    public struct ROIData
    {
        public ROIData()
        {

        }

        public HashSet<(int x, int y)> Coordinates;
        public MeanColorCalculator Calculator;
        public int Radius;
    }

    public struct GenerationData
    {
        public struct SingleGenerationData
        {
            public double BlurSigma { get; init; } = 1;
            public int MinWidth { get; init; } = 1;
            public int MaxWidth { get; init; } = 1;
            public int LocalIterations { get; init; } = 1000;
            public int DispersionTolerance { get; init; } = 300;

            public SingleGenerationData(double sigma, int min_w, int max_w, int iterations, int dispersion)
            {
                BlurSigma = sigma;
                MinWidth = min_w;
                MaxWidth = max_w;
                LocalIterations = iterations;
                DispersionTolerance = dispersion;
            }
        }

        public int Generations { get; init; }

        public Dictionary<int, SingleGenerationData> Data { get; init; }

        public GenerationData(int generations, int maxRadius, int width, int height)
        {
            Generations = generations;
            Data = new Dictionary<int, SingleGenerationData>();

            double minValue = 0.1;
            double interval = (1.0 - minValue) / generations;

            for (int gen = 0; gen < generations; gen++)
            {
                double factor_up = 1 - (gen) * interval;
                double factor_down = 1 - (gen + 1) * interval;
                double factor_scaled = 1 - (gen * 1.0 / generations);

                double blurSigma = Math.Round(maxRadius * factor_scaled);
                int minWidth = Convert.ToInt32(maxRadius * factor_down);
                int maxWidth = Convert.ToInt32(maxRadius * factor_up);
                int localIterations = Convert.ToInt32((width * height) / (Math.Pow((2 * maxWidth + 1), 2)));
                int dispersion = Convert.ToInt32(1500 * factor_up);

                Data.Add(gen, new SingleGenerationData(blurSigma, minWidth, maxWidth, localIterations, dispersion));
            }
        }
    }

    public class TracerSerializer
    {
        public static readonly TracerSerializer DefaultTracer = new TracerSerializer()
        {
            Genetaions = 7,
            BrushRadius = (3, 40),
            StrokeLength = (0, 50),
            SegmentsCount = (0, 3),
        };

        public int Genetaions { get; init; }

        public (int min, int max) BrushRadius { get; init; }

        public (int min, int max) StrokeLength { get; init; }

        public (int min, int max) SegmentsCount { get; init; }
    }

    public class Tracer
    {
        private string outputPath = "C:\\Users\\skura\\source\\repos\\ArtGenerator\\ArtModel\\Output";

        public GenerationData _generationData { get; private set; }

        private int Genetaions { get; init; }

        private (int min, int max) BrushRadius { get; init; }

        private (int min, int max) StrokeLength { get; init; }

        private (int min, int max) SegmentsCount { get; init; }

        private ArtBitmap _origBm;

        private ArtBitmap _artBm;

        private int _randomSeed = Guid.NewGuid().GetHashCode();

        public Tracer(ArtBitmap originalCanvas, ArtBitmap artificialCanvas, TracerSerializer tracerSerializer)
        {
            Genetaions = tracerSerializer.Genetaions;
            BrushRadius = tracerSerializer.BrushRadius;
            StrokeLength = tracerSerializer.StrokeLength;
            SegmentsCount = tracerSerializer.SegmentsCount;

            _origBm = originalCanvas;
            _artBm = artificialCanvas;

            _generationData = new GenerationData(Genetaions, BrushRadius.max, originalCanvas.Width, originalCanvas.Height);
        }

        public void GenerateArtByLayers()
        {
            // Поколения
            for (int gen = 0; gen < Genetaions; gen++)
            {
                SingleGenerationData localData = _generationData.Data[gen];

                RandomPoolGenerator pool = new RandomPoolGenerator(_origBm.Width, _origBm.Height, _randomSeed);

                double[,] brightnessMap = BrightnessMap.GetBrightnessMap(_origBm, localData.BlurSigma);

                int counter = 0;
                // Итерации в рамках поколения
                for (int iteration = 0; iteration < localData.LocalIterations; iteration++)
                {
                    pool.GetFromPool(out var coordinates);
                    int x = coordinates.x;
                    int y = coordinates.y;

                    TracingResult path = GetSegmentedTracePath(gen, (x, y), brightnessMap);
                    pool.RemoveFromPool(path.Coordinates);
                    WritePixels(path.Coordinates, path.MeanColor);

                    if (counter % 100 == 0)
                    {
                        _artBm.Save(outputPath, "Artificial" + counter);
                    }
                    counter++;
                }


                _artBm.Save(outputPath, $"Generation_{gen}");
            }
        }

        public TracingResult GetSegmentedTracePath(int genNum, (int x, int y) p1, double[,] brightnessMap)
        {
            int r_min = _generationData.Data[genNum].MinWidth;
            int r_max = _generationData.Data[genNum].MaxWidth;
            double tolerance = _generationData.Data[genNum].DispersionTolerance;

            ROIData roi = GetROI(genNum, p1);

            MeanColorCalculator segmentedCalculator = roi.Calculator;
            HashSet<(int x, int y)> segmentedPathCoordinates = roi.Coordinates;

            //int segments = SegmentsCount.max;
            int segmentsMax = 2;



            (int x, int y) currentSegmentPoint = p1;
            Color currentMeanColor = Color.White;



            for (int seg = 0; seg <= segmentsMax; seg++)
            {
                // Добавить двоичный поиск
                int strokeLength = StrokeLength.max;

                while (strokeLength > StrokeLength.min)
                {
                    double angle = brightnessMap[currentSegmentPoint.y, currentSegmentPoint.x];
                    (int x, int y) p2 = PointOffset(currentSegmentPoint, angle, strokeLength);
                    TracingPath path = GetPath(currentSegmentPoint, p2, segmentedPathCoordinates, segmentedCalculator, roi.Radius);

                    HashSet<(int x, int y)> newPath = path.Coordinates;
                    MeanColorCalculator newCalc = path.Calculator;

                    // Дисперсия в норме, значит мазок сегмента можно закончить (т.к. идём с конца).
                    if (path.Dispersion < tolerance)
                    {
                        segmentedPathCoordinates = newPath;
                        segmentedCalculator = newCalc;
                        currentSegmentPoint = p2;
                        currentMeanColor = path.MeanColor;
                        break;
                    }
                    else
                    {
                        strokeLength -= 1;

                        if (strokeLength == StrokeLength.min)
                        {
                            return new TracingResult(segmentedPathCoordinates, segmentedCalculator.GetMeanColor());
                        }
                    }
                }
            }

            return new TracingResult(segmentedPathCoordinates, segmentedCalculator.GetMeanColor());
        }

        private (int x, int y) PointOffset((int x, int y) p, double angle, double length)
        {
            angle = (angle + Math.PI / 2) % (2 * Math.PI);

            return (
                Math.Clamp(p.x + (int)(length * Math.Cos(angle)), 0, _origBm.Width - 1),
                Math.Clamp(p.y + (int)(length * Math.Sin(angle)), 0, _origBm.Height - 1));
        }

        private TracingPath GetPath(
            (int x, int y) p1,
            (int x, int y) p2,
            HashSet<(int x, int y)> segmentedPathCoordinates,
            MeanColorCalculator segmentedCalc,
            int radius)
        {
            MeanColorCalculator localCalc = segmentedCalc.Copy();

            HashSet<(int x, int y)> localPathCoordinates = new();
            localPathCoordinates.UnionWith(segmentedPathCoordinates);

            int dx = Math.Abs(p2.x - p1.x);
            int dy = Math.Abs(p2.y - p1.y);

            int sx = (p1.x < p2.x) ? 1 : -1;
            int sy = (p1.y < p2.y) ? 1 : -1;

            int err = dx - dy;

            while (true)
            {
                if (p1.x >= 0 && p1.x < _origBm.Width && p1.y >= 0 && p1.y < _origBm.Height)
                {
                    CircleMaskResult circle = StrokeCircleMask.ApplyCircleMask(_origBm, p1.x, p1.y, radius);
                    foreach (var c in circle.Coordinates)
                    {
                        if (!localPathCoordinates.Contains((c.x, c.y)))
                        {
                            localPathCoordinates.Add((c.x, c.y));
                            localCalc.AddColor(_origBm[c.x, c.y]);
                        }
                    }
                }

                if (p1.x == p2.x && p1.y == p2.y)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err = err - dy;
                    p1.x = p1.x + sx;
                }
                if (e2 < dx)
                {
                    err = err + dx;
                    p1.y = p1.y + sy;
                }

                // Пропуск пикселей
                /*for (int i = 0; i < StepOffset - 1; i++)
                {
                    if (p1.x == p2.x && p1.y == p2.y)
                        break;

                    p1.x += sx;
                    p1.y += sy;
                }*/
            }

            Color meanColor = localCalc.GetMeanColor();
            double dispersion = StrokeUtils.GetDispersion(_origBm, localPathCoordinates, meanColor);

            return new TracingPath()
            {
                Coordinates = localPathCoordinates,
                MeanColor = meanColor,
                Dispersion = dispersion,
                Calculator = localCalc
            };
        }

        private ROIData GetROI(int genNum, (int x, int y) point)
        {
            // В будущем добавить двоичный поиск

            int x = point.x;
            int y = point.y;

            int r_min = _generationData.Data[genNum].MinWidth;
            int r_max = _generationData.Data[genNum].MaxWidth;
            double tolerance = _generationData.Data[genNum].DispersionTolerance;

            int r_curr = r_max;

            while (r_curr >= r_min)
            {
                CircleMaskResult circle = StrokeCircleMask.ApplyCircleMask(_origBm, x, y, r_curr);
                MeanColorCalculator calc = new MeanColorCalculator(_origBm, circle.Coordinates);
                double dispesion = StrokeUtils.GetDispersion(_origBm, circle.Coordinates, calc.GetMeanColor());

                if (dispesion < tolerance || r_curr == r_min)
                {
                    return new ROIData()
                    {
                        Coordinates = circle.Coordinates,
                        Calculator = calc,
                        Radius = r_curr
                    };
                }
                else
                {
                    r_curr -= 1;
                }
            }

            throw new NotImplementedException();

            int MiddleRadiusUp(int r_cur, int r_max)
            {
                return Convert.ToInt32(r_cur + (r_max - r_cur) / 2);
            }

            int MiddleRadiusDown(int r_cur, int r_min)
            {
                return Convert.ToInt32(r_cur - (r_cur - r_min) / 2);
            }
        }

        private void WritePixels(HashSet<(int x, int y)> coordonates, Color color)
        {
            foreach (var c in coordonates)
            {
                _artBm[c.x, c.y] = color;
            }
        }


    }
}
