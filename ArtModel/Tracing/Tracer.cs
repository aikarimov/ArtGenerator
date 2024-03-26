using ArtModel.ImageModel.ImageProccessing;
using ArtModel.ImageProccessing;
using ArtModel.MathLib;
using ArtModel.StrokeLib;
using System.Drawing;
using ArtModel.Core;
using ArtModel.Tracing.PathTracing;
using System.Text;
using System.Collections;
using System.Drawing.Imaging;

namespace ArtModel.Tracing
{
    public class Tracer : IEnumerable<ArtBitmap>
    {
        private ArtBitmap _origBm;

        private PathSettings _pathSettings;

        private int Genetaions;

        private ArtModelSerializer _artModelSerializer;

        public Tracer(ArtBitmap originalCanvas, ArtModelSerializer serealizer, PathSettings pathSettings)
        {
            _artModelSerializer = serealizer;
            _pathSettings = pathSettings;
            Genetaions = serealizer.GenerationsNumber;

            _origBm = originalCanvas;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<ArtBitmap> GetEnumerator()
        {
            _origBm.Save(_pathSettings.OutputPath, "ArtOriginal");
            StrokeLibrary strokeLibrary = new StrokeLibrary(_pathSettings.LibraryPath, _artModelSerializer, 1);

            ArtBitmap artificial_render = new ArtBitmap(_origBm.Width, _origBm.Height).FillColor(Color.White);
            ArtBitmap artificial_model = new ArtBitmap(_origBm.Width, _origBm.Height).FillColor(Color.White);
            ArtBitmap _shapesBm = new ArtBitmap(_origBm.Width, _origBm.Height).FillColor(Color.White);
            ArtBitmap _frameBm = new ArtBitmap(_origBm.Width, _origBm.Height).FillColor(Color.White);

            TracerPointDecider decider = new TracerPointDecider(_origBm, artificial_render, 1);

            for (int gen = 0; gen < Genetaions; gen++)
            {
                ArtGeneration localData = _artModelSerializer.Generations[gen];

                ArtBitmap blurredOriginalMap = GaussianBlur.ApplyBlur(_origBm, localData.BlurSigma);
                double[,] blurredBrightnessMap = GaussianBlur.ApplyBlurToBrightnessMap(BrightnessMap.GetBrightnessMap(blurredOriginalMap), localData.BlurSigma);

                for (int iteration = 0; iteration < localData.Iterations; iteration++)
                {
                    (int x, int y) coordinates = DecideCoordinates(decider);

                    TracingResult tracingResult = GetSegmentedTracePath(localData, blurredOriginalMap, coordinates, blurredBrightnessMap);

                    Stroke classified = strokeLibrary.ClassifyStroke(tracingResult, localData);

                    SetupStroke(classified, tracingResult, strokeLibrary);

                    WritePixelsPre(artificial_model, tracingResult.Coordinates, tracingResult.MeanColor, decider);
                    WritePixelsFromStroke(artificial_render, classified, tracingResult, coordinates, decider);

                    artificial_render.Save(_pathSettings.OutputPath, $"gen_{gen}_iter1");
                    artificial_model.Save(_pathSettings.OutputPath, $"gen_{gen}_iter2");

                    yield return artificial_render;

                    /*foreach (var tr in FilterTracingResult(tracingResult))
                    {
                        
                    } */   
                }

                artificial_render.Save(_pathSettings.OutputPath, $"Generation_{gen}");
                artificial_model.Save(_pathSettings.OutputPath, $"Generation2_{gen}");

                yield return artificial_render;
            }
        }

        private (int x, int y) DecideCoordinates(TracerPointDecider decider)
        {
            return decider.GetRandomPointPool();
            if (decider.Pool.PoolPercent() > 0.05)
            {
                var point = decider.GetRandomPointPool();
                return point;
            }
            else
            {
                var point = decider.GetMaxDispersionPoint();
                return point;
            }
        }

        // Обработка результата, приведение к более оптимальному
        private TracingResult[] FilterTracingResult(TracingResult tracingResult)
        {
            double points = tracingResult.SP.GetP(StrokeProperty.Points);

            if (points == 2)
            {
                double len = tracingResult.SP.GetP(StrokeProperty.Length);

                // Превращаем слишком короткие мазки в точку
                if (len <= 5)
                {
                    tracingResult.SP.SetP(StrokeProperty.Points, 1);
                }

                return [tracingResult];
            }

            if (points == 3)
            {
                double len = tracingResult.SP.GetP(StrokeProperty.Length);
                double angle = tracingResult.SP.GetP(StrokeProperty.Angle1);
                double fraction = tracingResult.SP.GetP(StrokeProperty.Fraction);

                // Превращаем слишком короткие мазки в точку
                if (Math.Abs(angle) <= 10)
                {
                    tracingResult.SP.SetP(StrokeProperty.Points, 2);
                }

                if (len <= 5)
                {
                    //tracingResult.SP.SetP(StrokeProperty.Points, 1);
                }

                return [tracingResult];
            }

            return [tracingResult];
        }

        private TracingResult GetSegmentedTracePath(ArtGeneration genData, ArtBitmap bitmap, (int x, int y) startingPoint, double[,] brightnessMap)
        {
            double tolerance = genData.DispersionBound;
            int lenMin = genData.StrokeLength_Min;
            int lenMax = genData.StrokeLength_Max;

            TracingResult tracingResult = new TracingResult();
            tracingResult.SP.SetP(StrokeProperty.Points, 1);

            CircleTracingResult roi = CircleTracer.TraceIterative(genData, bitmap, startingPoint);
            MeanColorCalculator segmentedCalculator = roi.Calculator;
            HashSet<(int x, int y)> segmentedPathCoordinates = roi.Coordinates;
            tracingResult.SP.SetP(StrokeProperty.Width, roi.Width);

            Dictionary<int, (int x, int y)> pathPoints = new() { { 1, startingPoint } };
            Dictionary<int, double> pathSegmentLengths = new();

            double segmentedLength = 0;

            // Условие, чтобы досрочно прервать формирование мазка
            bool cancel_stroke_path = false;

            // Построение каждого сегмента
            // Начинаем строить вторую точку, одна и так есть 100%
            for (int segmentPoint = 2; segmentPoint <= 2; segmentPoint++)
            {
                if (cancel_stroke_path) { break; }

                var currPoint = pathPoints[segmentPoint - 1];

                double newAngle;
                switch (segmentPoint)
                {
                    // Если строится 2я точка, то это просто прямой мазок. Не нужна доп. обработка
                    case 2:
                        newAngle = NormalAngle(brightnessMap[currPoint.y, currPoint.x]);
                        tracingResult.MainAbsAngle = newAngle;
                        break;
                    // Если строится 3+ точка, надо смотреть на угол
                    default:
                        var prevPoint = pathPoints[segmentPoint - 2];
                        var angles = GetNewAngle(brightnessMap[currPoint.y, currPoint.x], prevPoint, currPoint);
                        newAngle = angles.absAngle;
                        tracingResult.SP.SetP(StrokeProperty.Angle1, angles.leftOrRight * angles.relAngle / Math.PI * 180);
                        break;
                }

                Task[] tasks = new Task[lenMax - lenMin + 1];
                TracingPath[] tracingPaths = new TracingPath[lenMax - lenMin + 1];
                for (int len = lenMin; len <= lenMax; len++)
                {
                    int index = len - lenMin;
                    int len_i = len;

                    tasks[index] = Task.Run(() =>
                    {
                        (int x, int y) offsetedPoint = VectorMath.PointOffsetClamp(currPoint, newAngle, len_i, bitmap.Width - 1, bitmap.Height - 1);
                        TracingPath path = LinearPathTracer.GetPath(bitmap, currPoint, offsetedPoint, segmentedPathCoordinates, segmentedCalculator, roi.Width);
                        path.Length = len_i;
                        path.EndPoint = offsetedPoint;
                        tracingPaths[index] = path;
                    });
                }

                Task.WaitAll(tasks);

                cancel_stroke_path = true;
                for (int i = tracingPaths.Length - 1; i >= 0; i--)
                {
                    TracingPath tpath = tracingPaths[i];

                    // Найден путь, у которого дисперсия допустимая
                    if (tpath.Dispersion <= tolerance)
                    {
                        // Путь является минимально возможным - значит окончить мазок
                        if (tpath.Length <= genData.StrokeLength_Min)
                        {
                            cancel_stroke_path = true;
                            break;
                        }
                        else
                        {
                            cancel_stroke_path = false;

                            segmentedLength += tpath.Length;

                            tracingResult.SP.SetP(StrokeProperty.Length, segmentedLength);
                            tracingResult.SP.SetP(StrokeProperty.LtoW, segmentedLength / roi.Width);
                            tracingResult.SP.SetP(StrokeProperty.Fraction, (segmentedLength - tpath.Length) * 100 / segmentedLength);

                            segmentedCalculator = tpath.Calculator;
                            segmentedPathCoordinates.UnionWith(tpath.Coordinates);


                            tracingResult.SP.SetP(StrokeProperty.Points, segmentPoint);
                            pathPoints.Add(segmentPoint, tpath.EndPoint);
                            pathSegmentLengths.Add(segmentPoint - 1, tpath.Length);
                            break;
                        }
                    }
                }
            }

            tracingResult.Coordinates = segmentedPathCoordinates;
            tracingResult.MeanColor = segmentedCalculator.GetMeanColor();

            return tracingResult;

            (double absAngle, double relAngle, int leftOrRight) GetNewAngle(double absAngle, (int x, int y) prevPoint, (int x, int y) currPoint)
            {
                // Взяли нормаль к углу
                absAngle = NormalAngle(absAngle);

                // Нашли 3ю точку по вектору
                (int x, int y) newPoint = VectorMath.PointOffset(currPoint, absAngle, 10);

                // Относительный угол между тремя точками, от 0 до PI
                double newRelAngle = VectorMath.AngleBy3Points(prevPoint, currPoint, newPoint);

                // Если относительный угол между сегментами мазка оказался острым, разворачиваем на 180 градусов всё
                if (newRelAngle < Math.PI / 2)
                {
                    newRelAngle += Math.PI;
                    absAngle += Math.PI;
                    newPoint = VectorMath.PointOffset(currPoint, absAngle, 10);
                }

                // На этом этапе у нас точно есть правильный расчётный сегмента из трёх точек

                int leftOrRight = -VectorMath.LeftOrRight(prevPoint, currPoint, newPoint);

                // Делаем относительный угол таким, чтобы подходил под классификацию в библиотеке. Так как отклонение в градусах идёт от угла PI
                newRelAngle = Math.PI - newRelAngle;

                return (absAngle, newRelAngle, leftOrRight);
            }

            double NormalAngle(double angle)
            {
                return (angle + Math.PI / 2) % (Math.Tau);
            }
        }

        private void SetupStroke(Stroke stroke, TracingResult tracingResult, StrokeLibrary strokeLibrary)
        {
            double resizeCoef = strokeLibrary.CalculateResizeCoefficient(tracingResult, stroke);
            stroke.Resize(resizeCoef);
            stroke.Rotate(tracingResult.MainAbsAngle);
        }

        private void WritePixelsPre(ArtBitmap map, HashSet<(int x, int y)> coordonates, Color color, TracerPointDecider decider = null)
        {
            
            foreach (var c in coordonates)
            {
                decider?.Pool.RemoveFromPool((c.x, c.y));
                map[c.x, c.y] = color;
            }
        }

        private void WritePixelsFromStroke(ArtBitmap original, Stroke stroke, TracingResult tracingResult, (int x, int y) globalPoint, TracerPointDecider decider = null)
        {
            globalPoint = PointOffset(globalPoint, (tracingResult.MainAbsAngle + Math.PI), tracingResult.SP.GetP(StrokeProperty.Width) / 2);

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
                        decider?.Pool.RemoveFromPool((globalX, globalY));
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

    }
}
