using ArtModel.ImageModel.ImageProccessing;
using ArtModel.MathLib;
using System.Drawing;
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


        public int Length;
        public (int x, int y) EndPoint;
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
        public double Dispersion;
    }

    public struct GenerationData
    {
        public struct SingleGenerationData
        {
            public double BlurSigma { get; init; } = 1;
            public (int min, int max) StrokeWidth { get; init; } = (4, 40);
            public (int min, int max) StrokeLength { get; init; } = (0, 50);
            public int LocalIterations { get; init; } = 1000;
            public int DispersionTolerance { get; init; } = 300;

            public SingleGenerationData(double sigma, (int, int) width, (int, int) length, int iterations, int dispersion)
            {
                BlurSigma = sigma;
                StrokeWidth = width;
                StrokeLength = length;
                LocalIterations = iterations;
                DispersionTolerance = dispersion;
            }
        }

        public int Generations { get; init; }

        public Dictionary<int, SingleGenerationData> Data { get; init; }

        public GenerationData(int generations, int width, int height)
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

                double blurSigma = Math.Round(40 * factor_scaled);
                (int, int) StrokeWidth = (Convert.ToInt32(40 * factor_down), Convert.ToInt32(40 * factor_up));
                (int, int) StrokeLength = (0, 50);

                int localIterations = Convert.ToInt32((width * height) / (Math.Pow((StrokeWidth.Item2 + 1), 2.5)));
                int dispersion = Convert.ToInt32(1500);

                Data.Add(gen, new SingleGenerationData(blurSigma, StrokeWidth, StrokeLength, localIterations, dispersion));
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
            SegmentsCount = (1, 3),
        };

        public int Genetaions { get; init; }

        public (int min, int max) BrushRadius { get; init; }

        public (int min, int max) StrokeLength { get; init; }

        public (int min, int max) SegmentsCount { get; init; }
    }

    public class Tracer
    {
        public GenerationData _generationData { get; private set; }

        private int Genetaions { get; init; }

        /*private (int min, int max) BrushRadius { get; init; }

        private (int min, int max) StrokeLength { get; init; }*/

        private (int min, int max) SegmentsCount { get; init; }

        private ArtBitmap _origBm;

        private string outpath;

        public Tracer(ArtBitmap originalCanvas, TracerSerializer tracerSerializer, string outputPath)
        {
            outpath = outputPath;

            Genetaions = tracerSerializer.Genetaions;
            //BrushRadius = tracerSerializer.BrushRadius;
            //StrokeLength = tracerSerializer.StrokeLength;
            SegmentsCount = tracerSerializer.SegmentsCount;

            _origBm = originalCanvas;

            _generationData = new GenerationData(Genetaions, originalCanvas.Width, originalCanvas.Height);
        }

        public void GenerateArtByLayers()
        {
            ArtBitmap artificial = new ArtBitmap(_origBm.Width, _origBm.Height);

            double[,] brightnessMap = BrightnessMap.GetBrightnessMap(_origBm);

            for (int gen = 0; gen < Genetaions; gen++)
            {
                SingleGenerationData localData = _generationData.Data[gen];
                RandomPoolGenerator pool = new RandomPoolGenerator(_origBm.Width, _origBm.Height);
                double[,] blurredBrightnessMap = GaussianBlur.ApplyBlur(brightnessMap, localData.BlurSigma);
                for (int iteration = 0; iteration < localData.LocalIterations; iteration++)
                {


                    if (pool.PoolAvaliable())
                    {
                        var coordinates = pool.GetFromPool();
                        int x = coordinates.x;
                        int y = coordinates.y;


                        TracingResult path = GetSegmentedTracePath(localData, _origBm, (x, y), blurredBrightnessMap);
                        pool.RemoveFromPool(path.Coordinates);
                        WritePixels(artificial, path.Coordinates, path.MeanColor);
                    }
                    else
                    {
                        break;
                    }
                }


                artificial.Save(outpath, $"Generation_{gen}");
            }
        }

        public TracingResult GetSegmentedTracePath(
            SingleGenerationData data,
            ArtBitmap bitmap,
            (int x, int y) startingPoint,
            double[,] blurredBrightnessMap)
        {
            double tolerance = data.DispersionTolerance;
            int lenMin = data.StrokeLength.min;
            int lenMax = data.StrokeLength.max;

            ROIData roi = GetROI(data, bitmap, startingPoint);

            MeanColorCalculator segmentedCalculator = roi.Calculator;
            HashSet<(int x, int y)> segmentedPathCoordinates = roi.Coordinates;

            (int x, int y) currentSegmentPoint = startingPoint;

            // Построение каждого сегмента
            for (int seg = SegmentsCount.min; seg <= SegmentsCount.max; seg++)
            {

                Task[] tasks = new Task[lenMax - lenMin + 1];
                TracingPath[] tracingPaths = new TracingPath[lenMax - lenMin + 1];
                double angle = blurredBrightnessMap[currentSegmentPoint.y, currentSegmentPoint.x];

                for (int len = lenMin; len <= lenMax; len++)
                {
                    int index = len - lenMin;
                    int currLen = len;

                    tasks[index] = Task.Run(() =>
                    {
                        (int x, int y) offsetedPoint = PointOffsetNormal(currentSegmentPoint, angle, currLen);
                        TracingPath path = GetPath(currentSegmentPoint, offsetedPoint, segmentedPathCoordinates, segmentedCalculator, roi.Radius);

                        path.Length = currLen;
                        path.EndPoint = offsetedPoint;

                        tracingPaths[index] = path;
                    });
                }

                Task.WaitAll(tasks);

                for (int i = tracingPaths.Length - 1; i >= 0; i--)
                {
                    TracingPath tpath = tracingPaths[i];
                    if (tpath.Dispersion <= tolerance)
                    {
                        // Значит, что минимальная дисперсия достигнута на 0-м мазке, а значит его нужно окончить
                        if (i == 0)
                        {
                            return new TracingResult(segmentedPathCoordinates, segmentedCalculator.GetMeanColor());
                        }
                        else
                        {
                            segmentedCalculator = tpath.Calculator;
                            segmentedPathCoordinates = tpath.Coordinates;

                            currentSegmentPoint = tpath.EndPoint;

                            break;
                        }

                    }
                }
            }

            return new TracingResult(segmentedPathCoordinates, segmentedCalculator.GetMeanColor());

            (int x, int y) PointOffsetNormal((int x, int y) p, double angle, double length)
            {
                angle = (angle + Math.PI / 2) % (2 * Math.PI);

                return (
                    Math.Clamp(p.x + (int)(length * Math.Cos(angle)), 0, bitmap.Width - 1),
                    Math.Clamp(p.y + (int)(length * Math.Sin(angle)), 0, bitmap.Height - 1));
            }
        }

        private TracingPath GetPath(
            (int x, int y) p1,
            (int x, int y) p2,
            HashSet<(int x, int y)> segmentedPathCoordinates,
            MeanColorCalculator segmentedCalc,
            int radius)
        {
            MeanColorCalculator localCalculator = segmentedCalc.Copy();
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
                            localCalculator.AddColor(_origBm[c.x, c.y]);
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
            }

            Color meanColor = localCalculator.GetMeanColor();
            double dispersion = StrokeUtils.GetDispersion(_origBm, localPathCoordinates, meanColor);

            return new TracingPath()
            {
                Coordinates = localPathCoordinates,
                MeanColor = meanColor,
                Dispersion = dispersion,
                Calculator = localCalculator
            };
        }

        private ROIData GetROI(SingleGenerationData data, ArtBitmap bitmap, (int x, int y) point)
        {
            int x = point.x;
            int y = point.y;
            int r_min = data.StrokeWidth.min;
            int r_max = data.StrokeWidth.max;
            double tolerance = data.DispersionTolerance;

            Task[] tasks = new Task[r_max - r_min + 1];
            ROIData[] rois = new ROIData[r_max - r_min + 1];

            for (int radius = r_min; radius <= r_max; radius++)
            {
                int index = radius - r_min;
                int rad = radius;

                tasks[index] = Task.Run(() =>
                {
                    CircleMaskResult circle = StrokeCircleMask.ApplyCircleMask(bitmap, x, y, rad);
                    MeanColorCalculator calc = new MeanColorCalculator(bitmap, circle.Coordinates);
                    double dispesion = StrokeUtils.GetDispersion(bitmap, circle.Coordinates, calc.GetMeanColor());
                    rois[index] = new ROIData()
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
                ROIData ro = rois[i];
                if (ro.Dispersion <= tolerance)
                {
                    return ro;
                }
            }

            return rois[0];
        }

        private void WritePixels(ArtBitmap map, HashSet<(int x, int y)> coordonates, Color color)
        {
            foreach (var c in coordonates)
            {
                map[c.x, c.y] = color;
            }
        }
    }
}
