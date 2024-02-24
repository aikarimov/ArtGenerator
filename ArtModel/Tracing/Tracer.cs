using ArtModel.ImageModel.ImageProccessing;
using ArtModel.ImageProccessing;
using ArtModel.MathLib;
using ArtModel.StrokeLib;
using System.Drawing;
using ArtModel.Core;

namespace ArtModel.Tracing
{
    public struct TracingPath
    {
        public MeanColorCalculator Calculator { get; set; }
        public HashSet<(int x, int y)> Coordinates { get; set; }
        public Color MeanColor { get; set; }
        public double Dispersion { get; set; }
        public int Length { get; set; }
        public (int x, int y) EndPoint { get; set; }
    }

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

            double[,] brightnessMap = BrightnessMap.GetBrightnessMap(_origBm);


            for (int gen = 0; gen < Genetaions; gen++)
            {
                RandomPoolGenerator pool = new RandomPoolGenerator(_origBm.Width, _origBm.Height);
                ArtGeneration localData = _artModelSerializer.Generations[gen];

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

                        WritePixelsPre(artificial_model, tracingResult.Coordinates, tracingResult.MeanColor, pool);
                        WritePixelsFromStroke(artificial_render, classified, tracingResult, pool, coordinates);

                        // artificial.Save(_outputPath, $"gen_{gen}_iter1");
                        //artificial_model.Save(_outputPath, $"gen_{gen}_iter2");
                    }
                    else
                    {
                        break;
                    }
                }


                artificial_render.Save(_outputPath, $"Generation_{gen}");
                artificial_model.Save(_outputPath, $"Generation2_{gen}");
            }
        }

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
                        TracingPath path = GetPath(bitmap, currentSegmentPoint, offsetedPoint, segmentedPathCoordinates, segmentedCalculator, roi.Width);
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
            (int x, int y) p1, (int x, int y) p2,
            HashSet<(int x, int y)> segmentedPathCoordinates, MeanColorCalculator segmentedCalc,
            int width)
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
                    CircleMaskResult circle = StrokeCircleMask.ApplyCircleMask(bitmap, p1.x, p1.y, width / 2);
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
            //pool.RemoveFromPool(coordonates);
            foreach (var c in coordonates)
            {
                map[c.x, c.y] = color;
            }
        }


        private void WritePixelsFromStroke(ArtBitmap original, Stroke stroke, TracingResult tracingResult, RandomPoolGenerator pool, (int x, int y) globalPoint)
        {
            globalPoint = PointOffset(globalPoint, (tracingResult.MainAngle + Math.PI), tracingResult.StrokeProperties.GetProperty(StrokeProperty.Width) / 2);

            stroke.Rotate(tracingResult.MainAngle);

            Color color = tracingResult.MeanColor;

            for (int x = 0; x < stroke.Width; x++)
            {
                for (int y = 0; y < stroke.Height; y++)
                {
                    int globalX = globalPoint.x - stroke.PivotPoint.x + x;
                    int globalY = globalPoint.y - stroke.PivotPoint.y + y;

                    Color strokeCol = stroke[x, y];
                    if (globalX >= 0 && globalX < original.Width && globalY >= 0 && globalY < original.Height && strokeCol.R < 255)
                    {
                        original[globalX, globalY] = CalculateAlpha(original[globalX, globalY], color, (255.0 - strokeCol.R) / 255.0);
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

            Color CalculateAlpha(in Color back, in Color front, in double a)
            {
                return Color.FromArgb(
                    Math.Clamp((int)(a * front.R + (1 - a) * back.R), 0, 255),
                    Math.Clamp((int)(a * front.G + (1 - a) * back.G), 0, 255),
                    Math.Clamp((int)(a * front.B + (1 - a) * back.B), 0, 255));
            }
        }
    }
}
