using ArtModel.ImageModel.ImageProccessing;
using ArtModel.ImageModel.Tracing.Circle;
using ArtModel.MathLib;
using ArtModel.StrokeLib;
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

    public struct GenerationData
    {
        public struct SingleGenerationData
        {
            public double BlurSigma { get; init; } = 1;
            public (int min, int max) StrokeWidth { get; init; } = (4, 40);
            public (int min, int max) StrokeLength { get; init; } = (0, 50);
            public int Iterations { get; init; } = 1000;
            public int Dispersion { get; init; } = 300;

            public SingleGenerationData(double sigma, (int, int) width, (int, int) length, int iterations, int dispersion)
            {
                BlurSigma = sigma;
                StrokeWidth = width;
                StrokeLength = length;
                Iterations = iterations;
                Dispersion = dispersion;
            }
        }

        public int Generations { get; init; }

        public Dictionary<int, SingleGenerationData> Data { get; init; }

        public GenerationData(ArtBitmap artBitmap, TracerSerializer tracerSerializer)
        {
            Generations = tracerSerializer.GenetaionsNumber;
            Data = new Dictionary<int, SingleGenerationData>();

            double minValue = 0.1;
            double interval = (1.0 - minValue) / Generations;

            for (int gen = 0; gen < Generations; gen++)
            {
                double factor_up = 1 - (gen) * interval;
                double factor_down = 1 - (gen + 1) * interval;
                double factor_scaled = 1 - (gen * 1.0 / Generations);

                double blurSigma = Math.Round(tracerSerializer.BlurSigma * factor_scaled);
                (int, int) StrokeWidth = (
                    Convert.ToInt32(tracerSerializer.StrokeWidth.min * factor_down),
                    Convert.ToInt32(tracerSerializer.StrokeWidth.max * factor_up));

                (int, int) StrokeLength = tracerSerializer.StrokeLength;

                int localIterations = Convert.ToInt32((artBitmap.Width * artBitmap.Height) / (Math.Pow((StrokeWidth.Item2 + 1), 2.5)));
                int dispersion = Convert.ToInt32(500);

                Data.Add(gen, new SingleGenerationData(blurSigma, StrokeWidth, StrokeLength, localIterations, dispersion));
            }
        }
    }

    public class TracerSerializer
    {
        public static readonly TracerSerializer DefaultTracer = new TracerSerializer()
        {
            GenetaionsNumber = 7,
            StrokeWidth = (4, 40),
            StrokeLength = (0, 50),
            SegmentsCount = 2,
            BlurSigma = 40,
        };

        public int GenetaionsNumber { get; init; }

        public (int min, int max) StrokeWidth { get; init; }

        public (int min, int max) StrokeLength { get; init; }

        public int SegmentsCount { get; init; }

        public int BlurSigma { get; init; }
    }

    public class Tracer
    {
        public GenerationData _generationData { get; private set; }

        private int Genetaions { get; init; }

        private (int min, int max) BrushRadius { get; init; }

        private (int min, int max) StrokeLength { get; init; }

        private int SegmentsCount { get; init; }

        private ArtBitmap _origBm;

        private string _outputPath;

        public Tracer(ArtBitmap originalCanvas, TracerSerializer tracerSerializer, string outputPath)
        {
            _outputPath = outputPath;

            Genetaions = tracerSerializer.GenetaionsNumber;
            BrushRadius = tracerSerializer.StrokeWidth;
            StrokeLength = tracerSerializer.StrokeLength;
            SegmentsCount = tracerSerializer.SegmentsCount;

            _origBm = originalCanvas;

            _generationData = new GenerationData(originalCanvas, tracerSerializer);
        }

        public void GenerateArtByLayers()
        {
            StrokeLibrary strokeLibrary = new StrokeLibrary(1);

            ArtBitmap artificial = new ArtBitmap(_origBm.Width, _origBm.Height);
            ArtBitmap artificial2 = new ArtBitmap(_origBm.Width, _origBm.Height);

            double[,] brightnessMap = BrightnessMap.GetBrightnessMap(_origBm);

            for (int gen = 0; gen < Genetaions; gen++)
            {
                SingleGenerationData localData = _generationData.Data[gen];
                RandomPoolGenerator pool = new RandomPoolGenerator(_origBm.Width, _origBm.Height);

                double[,] blurredBrightnessMap = GaussianBlur.ApplyBlur(brightnessMap, localData.BlurSigma);
                ArtBitmap blurredOriginalMap = GaussianBlur.ApplyBlur(_origBm, localData.BlurSigma);

                for (int iteration = 0; iteration < localData.Iterations; iteration++)
                {
                    if (pool.PoolAvaliable())
                    {
                        (int x, int y) coordinates = pool.GetFromPool();

                        TracingResult tracingResult = GetSegmentedTracePath(localData, blurredOriginalMap, coordinates, blurredBrightnessMap);
                        Stroke classified = strokeLibrary.ClassifyStroke(tracingResult, localData);

                        double resizeCoef = strokeLibrary.CalculateResizeCoefficient(tracingResult, classified);
                        classified.Resize(resizeCoef);

                        WritePixelsPre(artificial2, tracingResult.Coordinates, tracingResult.MeanColor, pool);
                        WritePixelsFromStroke(artificial, classified, tracingResult, pool, coordinates);

                        //artificial.Save(_outputPath, $"gen_{gen}_iter1");
                        // artificial2.Save(_outputPath, $"gen_{gen}_iter2");
                    }
                    else
                    {
                        break;
                    }
                }


                artificial.Save(_outputPath, $"Generation_{gen}");
                artificial2.Save(_outputPath, $"Generation2_{gen}");
            }
        }

        public TracingResult GetSegmentedTracePath(SingleGenerationData genData, ArtBitmap bitmap, (int x, int y) startingPoint, double[,] brightnessMap)
        {
            TracingResult tracingResult = new TracingResult();
            tracingResult.StrokeProperties.SetProperty(StrokeProperty.Points, 1);

            double tolerance = genData.Dispersion;
            int lenMin = genData.StrokeLength.min;
            int lenMax = genData.StrokeLength.max;

            CircleTracingResult roi = CircleTracer.TraceIterative(genData, bitmap, startingPoint);
            tracingResult.StrokeProperties.SetProperty(StrokeProperty.Width, 2 * roi.Radius);

            MeanColorCalculator segmentedCalculator = roi.Calculator;
            HashSet<(int x, int y)> segmentedPathCoordinates = roi.Coordinates;

            (int x, int y) currentSegmentPoint = startingPoint;
            double segmentedLength = 2 * roi.Radius;

            // Построение каждого сегмента
            for (int segment = 1; segment < SegmentsCount; segment++)
            {


                Task[] tasks = new Task[lenMax - lenMin + 1];
                TracingPath[] tracingPaths = new TracingPath[lenMax - lenMin + 1];

                double newAngle = brightnessMap[currentSegmentPoint.y, currentSegmentPoint.x];
                newAngle = AngleNormal(newAngle);

                switch (segment)
                {
                    case 1:
                        tracingResult.MainAngle = newAngle;
                        break;
                    case 2:
                        tracingResult.StrokeProperties.SetProperty(StrokeProperty.Angle, newAngle);
                        break;
                }


                for (int len = lenMin; len <= lenMax; len++)
                {
                    int index = len - lenMin;
                    int len_i = len;

                    tasks[index] = Task.Run(() =>
                    {
                        (int x, int y) offsetedPoint = PointOffsetNormal(currentSegmentPoint, newAngle, len_i);
                        TracingPath path = GetPath(bitmap, currentSegmentPoint, offsetedPoint, segmentedPathCoordinates, segmentedCalculator, roi.Radius);
                        path.Length = len_i;
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
                        segmentedLength += tpath.Length;
                        tracingResult.StrokeProperties.SetProperty(StrokeProperty.Length, segmentedLength);


                        // Значит, что минимальная дисперсия достигнута на мазке минимальной длины, а значит его нужно окончить, ведь это точка
                        if (i < 3)
                        {
                            tracingResult.StrokeProperties.SetProperty(StrokeProperty.Points, 1);


                            tracingResult.Coordinates = segmentedPathCoordinates;
                            tracingResult.MeanColor = segmentedCalculator.GetMeanColor();

                            return tracingResult;
                        }
                        else
                        {
                            tracingResult.StrokeProperties.SetProperty(StrokeProperty.Points, 2);
                            segmentedCalculator = tpath.Calculator;
                            segmentedPathCoordinates = tpath.Coordinates;
                            currentSegmentPoint = tpath.EndPoint;
                            break;
                        }

                    }
                }
            }

            tracingResult.Coordinates = segmentedPathCoordinates;
            tracingResult.MeanColor = segmentedCalculator.GetMeanColor();

            return tracingResult;


            double AngleNormal(double angle)
            {
                double newAngle = (angle + Math.PI / 2) % (Math.Tau);
                if ((angle - newAngle) % (Math.Tau) > (Math.PI / 2))
                {
                    newAngle = (newAngle + Math.PI) % (Math.Tau);
                }
                return newAngle;
            }

            (int x, int y) PointOffsetNormal((int x, int y) p, double angle, double length)
            {
                return (
                    Math.Clamp(p.x + (int)(length * Math.Cos(angle)), 0, bitmap.Width - 1),
                    Math.Clamp(p.y + (int)(length * Math.Sin(angle)), 0, bitmap.Height - 1));
            }
        }

        private TracingPath GetPath(
            ArtBitmap bitmap,
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
                if (p1.x >= 0 && p1.x < bitmap.Width && p1.y >= 0 && p1.y < bitmap.Height)
                {
                    CircleMaskResult circle = StrokeCircleMask.ApplyCircleMask(bitmap, p1.x, p1.y, radius);
                    foreach (var c in circle.Coordinates)
                    {
                        if (!localPathCoordinates.Contains((c.x, c.y)))
                        {
                            localPathCoordinates.Add((c.x, c.y));
                            localCalculator.AddColor(bitmap[c.x, c.y]);
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
            double dispersion = StrokeUtils.GetDispersion(bitmap, localPathCoordinates, meanColor);

            return new TracingPath()
            {
                Coordinates = localPathCoordinates,
                MeanColor = meanColor,
                Dispersion = dispersion,
                Calculator = localCalculator
            };
        }

        private void WritePixelsPre(ArtBitmap map, HashSet<(int x, int y)> coordonates, Color color, RandomPoolGenerator pool)
        {
            // pool.RemoveFromPool(coordonates);
            foreach (var c in coordonates)
            {
                map[c.x, c.y] = color;
            }
        }


        private void WritePixelsFromStroke(ArtBitmap original, Stroke stroke, TracingResult tracingResult, RandomPoolGenerator pool, (int x, int y) globalPoint)
        {
            globalPoint = PointOffset(globalPoint, (tracingResult.MainAngle + Math.PI), tracingResult.StrokeProperties.GetProperty(StrokeProperty.Width) / 2);

            //stroke.Save(_outputPath, "stroke1");

            stroke.Rotate(tracingResult.MainAngle, out (int x, int y) pivot);

            //stroke.Save(_outputPath, "stroke2");

            Color color = tracingResult.MeanColor;

            for (int x = 0; x < stroke.Width; x++)
            {
                for (int y = 0; y < stroke.Height; y++)
                {

                    int globalX = globalPoint.x - pivot.x + x;
                    int globalY = globalPoint.y - pivot.y + y;

                    if (globalX >= 0 && globalX < original.Width && globalY >= 0 && globalY < original.Height && stroke[x, y].R < 240)
                    {
                        original[globalX, globalY] = Color.FromArgb(stroke[x, y].R, color.R, color.G, color.B);
                        pool.RemoveFromPool((globalX, globalY));
                    }
                }
            }

            (int x, int y) PointOffset((int x, int y) p, double angle, double length)
            {
                return (
                    Math.Clamp(p.x + (int)(length * Math.Cos(angle)), 0, original.Width - 1),
                    Math.Clamp(p.y + (int)(length * Math.Sin(angle)), 0, original.Height - 1));
            }
        }
    }
}
