using ArtModel.ImageModel.ImageProccessing;
using ArtModel.ImageProccessing;
using ArtModel.MathLib;
using ArtModel.StrokeLib;
using System.Drawing;
using ArtModel.Core;
using ArtModel.Tracing.PathTracing;

namespace ArtModel.Tracing
{

    public class Tracer
    {
        private ArtBitmap _origBm;

        private string _outputPath;

        private int Genetaions;

        private ArtModelSerializer _artModelSerializer;

        public Tracer(ArtBitmap originalCanvas, ArtModelSerializer serealizer, string outputPath)
        {
            _artModelSerializer = serealizer;
            _outputPath = outputPath;
            Genetaions = serealizer.GenerationsNumber;
            _origBm = originalCanvas;
        }

        public void GenerateArtByLayers()
        {
            _origBm.Save(_outputPath, "ArtOriginal");
            StrokeLibrary strokeLibrary = new StrokeLibrary(1);

            ArtBitmap artificial_render = new ArtBitmap(_origBm.Width, _origBm.Height);
            artificial_render.FillColor(Color.White);

            ArtBitmap artificial_model = new ArtBitmap(_origBm.Width, _origBm.Height);
            artificial_model.FillColor(Color.White);

            RandomPoolGenerator pool = new RandomPoolGenerator(_origBm.Width, _origBm.Height);

            for (int gen = 0; gen < Genetaions; gen++)
            {
                ArtGeneration localData = _artModelSerializer.Generations[gen];

                ArtBitmap blurredOriginalMap = GaussianBlur.ApplyBlur(_origBm, localData.BlurSigma);
                double[,] blurredBrightnessMap = GaussianBlur.ApplyBlurToBrightnessMap(BrightnessMap.GetBrightnessMap(blurredOriginalMap), localData.BlurSigma);

                for (int iteration = 0; iteration < localData.Iterations; iteration++)
                {
                    if (pool.PoolAvaliable())
                    {
                        (int x, int y) coordinates = pool.GetFromPool();

                        TracingResult tracingResult = GetSegmentedTracePath(localData, blurredOriginalMap, coordinates, blurredBrightnessMap);
                        Stroke classified = strokeLibrary.ClassifyStroke(tracingResult, localData);

                        SetupStroke(classified, tracingResult, strokeLibrary);

                        WritePixelsPre(artificial_model, tracingResult.Coordinates, tracingResult.MeanColor);
                        WritePixelsFromStroke(artificial_render, classified, tracingResult, coordinates, pool);

                        artificial_render.Save(_outputPath, $"gen_{gen}_iter1");
                        artificial_model.Save(_outputPath, $"gen_{gen}_iter2");
                    }
                    else
                    {
                        break;
                    }
                }

                artificial_render.Save(_outputPath, $"Generation_{gen}");
                artificial_model.Save(_outputPath, $"Generation2_{gen}");
            }


            // Детальная обработка
            ArtGeneration detailedGen = new ArtGeneration()
            {
                Iterations = 1,
                StrokeWidth_Min = 4,
                StrokeWidth_Max = 5,
                StrokeLength_Min = 0,
                StrokeLength_Max = 20,
                BlurSigma = 1,
                DispersionBound = 100,
            };
            ;
            ArtBitmap blurredDetailed = GaussianBlur.ApplyBlur(_origBm, detailedGen.BlurSigma);
            double[,] blurredBrightnessdDetailed = GaussianBlur.ApplyBlurToBrightnessMap(BrightnessMap.GetBrightnessMap(blurredDetailed), detailedGen.BlurSigma);

            var samples = PreciseSampling(_origBm, artificial_render);

            foreach (var point in samples)
            {
                TracingResult tracingResult = GetSegmentedTracePath(detailedGen, blurredDetailed, point, blurredBrightnessdDetailed);
                Stroke classified = strokeLibrary.ClassifyStroke(tracingResult, detailedGen);

                SetupStroke(classified, tracingResult, strokeLibrary);

                WritePixelsPre(artificial_model, tracingResult.Coordinates, tracingResult.MeanColor);
                WritePixelsFromStroke(artificial_render, classified, tracingResult, point);
            }

            artificial_render.Save(_outputPath, $"Generation_detailed");
            artificial_model.Save(_outputPath, $"Generation_detailed2");
        }


        long time_path = 0;
        long time_path2 = 0;

        public TracingResult GetSegmentedTracePath(ArtGeneration genData, ArtBitmap bitmap, (int x, int y) startingPoint, double[,] brightnessMap)
        {
            TracingResult tracingResult = new TracingResult();
            tracingResult.StrokeProperties.SetProperty(StrokeProperty.Points, 1);

            double tolerance = genData.DispersionBound;
            int lenMin = genData.StrokeLength_Min;
            int lenMax = genData.StrokeLength_Max;

            CircleTracingResult roi = CircleTracer.TraceIterative(genData, bitmap, startingPoint);
            tracingResult.StrokeProperties.SetProperty(StrokeProperty.Width, roi.Width);

            MeanColorCalculator segmentedCalculator = roi.Calculator;
            HashSet<(int x, int y)> segmentedPathCoordinates = roi.Coordinates;

            (int x, int y) currentSegmentPoint = startingPoint;
            double segmentedLength = roi.Width;

            // Построение каждого сегмента
            for (int segment = 1; segment < 2; segment++)
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
                        /* case 2:
                             tracingResult.StrokeProperties.SetProperty(StrokeProperty.Angle, newAngle);
                             break;*/
                }

                for (int len = lenMin; len <= lenMax; len++)
                {
                    int index = len - lenMin;
                    int len_i = len;

                    tasks[index] = Task.Run(() =>
                    {
                        (int x, int y) offsetedPoint = VectorMath.PointOffsetClamp(currentSegmentPoint, newAngle, len_i, bitmap.Width - 1, bitmap.Height - 1);

                        var watch2 = System.Diagnostics.Stopwatch.StartNew();
                        TracingPath path = LinearPathTracer.GetPath2(bitmap, currentSegmentPoint, offsetedPoint, segmentedPathCoordinates, segmentedCalculator, roi.Width);
                        watch2.Stop();
                        time_path2 += watch2.ElapsedMilliseconds;

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
                        if (tpath.Length <= genData.StrokeLength_Min)
                        {
                            tracingResult.StrokeProperties.SetProperty(StrokeProperty.Points, 1);

                            segmentedCalculator = tpath.Calculator;
                            segmentedPathCoordinates.UnionWith(tpath.Coordinates);

                            tracingResult.Coordinates = segmentedPathCoordinates;
                            tracingResult.MeanColor = segmentedCalculator.GetMeanColor();

                            return tracingResult;
                        }
                        else
                        {
                            tracingResult.StrokeProperties.SetProperty(StrokeProperty.Points, 2);

                            segmentedCalculator = tpath.Calculator;
                            segmentedPathCoordinates.UnionWith(tpath.Coordinates);

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

                /*if ((angle - newAngle) % (Math.Tau) > (Math.PI / 2))
                {
                    newAngle = (newAngle + Math.PI) % (Math.Tau);
                }*/
                return newAngle;
            }

        }


        private void SetupStroke(Stroke stroke, TracingResult tracingResult, StrokeLibrary strokeLibrary)
        {
            double resizeCoef = strokeLibrary.CalculateResizeCoefficient(tracingResult, stroke);
            stroke.Resize(resizeCoef);
            stroke.Rotate(tracingResult.MainAngle);
        }

        private void WritePixelsPre(ArtBitmap map, HashSet<(int x, int y)> coordonates, Color color, RandomPoolGenerator pool = null)
        {
            //pool?.RemoveFromPool(coordonates);
            foreach (var c in coordonates)
            {
                map[c.x, c.y] = color;
            }
        }

        private void WritePixelsFromStroke(ArtBitmap original, Stroke stroke, TracingResult tracingResult, (int x, int y) globalPoint, RandomPoolGenerator pool = null)
        {
            globalPoint = PointOffset(globalPoint, (tracingResult.MainAngle + Math.PI), tracingResult.StrokeProperties.GetProperty(StrokeProperty.Width) / 2);

            Color color = tracingResult.MeanColor;

            for (int x = 0; x < stroke.Width; x++)
            {
                for (int y = 0; y < stroke.Height; y++)
                {
                    int globalX = globalPoint.x - stroke.PivotPoint.x + x;
                    int globalY = globalPoint.y - stroke.PivotPoint.y + y;

                    Color strokeCol = stroke[x, y];
                    if (original.IsInside(globalX, globalY) && strokeCol.R < 255)
                    {
                        original[globalX, globalY] = CalculateAlpha(original[globalX, globalY], color, (255.0 - strokeCol.R) / 255.0);
                        pool?.RemoveFromPool((globalX, globalY));
                    }
                }
            }

            (int x, int y) PointOffset((int x, int y) p, double angle, double length)
            {
                return (
                    Math.Clamp(p.x + (int)(length * Math.Cos(angle)), 0, original.Width - 1),
                    Math.Clamp(p.y + (int)(length * Math.Sin(angle)), 0, original.Height - 1));
            }

            Color CalculateAlpha(in Color back, in Color front, in double a)
            {
                return Color.FromArgb(
                    Math.Clamp((int)(a * front.R + (1 - a) * back.R), 0, 255),
                    Math.Clamp((int)(a * front.G + (1 - a) * back.G), 0, 255),
                    Math.Clamp((int)(a * front.B + (1 - a) * back.B), 0, 255));
            }
        }

        // Точки с наибольшей ошибкой в квадратах с наибольшей ошибкой
        private HashSet<(int x, int y)> PreciseSampling(ArtBitmap original, ArtBitmap artificial)
        {
            int square_size = 100;
            int max_error_squares = 100; // Сколько взять квадратов
            int max_error_points = 1500; // Точки на квадрат
            int sample_size = square_size;

            HashSet<(int x, int y)> maxErrorPoints = new HashSet<(int x, int y)>();

            Dictionary<(int x, int y), double> errorCache = new Dictionary<(int x, int y), double>();

            List<Rectangle> squares = GetSquares();

            squares.Sort((s1, s2) => CalculateSquareError(s2).CompareTo(CalculateSquareError(s1)));

            Parallel.ForEach(squares.Take(max_error_squares), square =>
            {
                List<(int x, int y)> squareMaxErrorPoints = GetPointsWithMaxErrorInSquare(square);

                lock (maxErrorPoints)
                {
                    foreach (var point in squareMaxErrorPoints)
                    {
                        maxErrorPoints.Add(point);
                    }
                }
            });

            return maxErrorPoints;

            List<(int x, int y)> GetPointsWithMaxErrorInSquare(Rectangle square)
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

                List<(int x, int y)> maxErrorPoints = new List<(int x, int y)>();
                maxErrorPoints.AddRange(allPointsInSquare.Take(max_error_points));

                return maxErrorPoints;
            }

            double CalculateSquareError(Rectangle square)
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
                            Color originalColor = original[x, y];
                            Color artificialColor = artificial[x, y];
                            double pixelColorDifference = ColorEuclideanDistance(originalColor, artificialColor);
                            errorCache[pixel] = pixelColorDifference;
                            totalError += pixelColorDifference;
                        }
                    }
                }

                return totalError;
            }

            double ColorEuclideanDistance(in Color color1, in Color color2)
            {
                double redDiff = color1.R - color2.R;
                double greenDiff = color1.G - color2.G;
                double blueDiff = color1.B - color2.B;
                return Math.Sqrt(redDiff * redDiff + greenDiff * greenDiff + blueDiff * blueDiff);
            }

            List<Rectangle> GetSquares()
            {
                List<Rectangle> list = new List<Rectangle>();

                for (int x = 0; x < original.Width; x += sample_size)
                {
                    for (int y = 0; y < original.Height; y += sample_size)
                    {
                        list.Add(new Rectangle(x, y, Math.Min(square_size, original.Width - x), Math.Min(square_size, original.Height - y)));
                    }
                }

                return list;
            }
        }
    }
}
