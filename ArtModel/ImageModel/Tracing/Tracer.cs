using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;

namespace ArtModel.ImageModel.Tracing
{
    public struct TracingPath
    {
        public TracingPath()
        {

        }

        public MeanColorCalculator Calculator;
        public List<(int x, int y)> Coordinates;
        public Color MeanColor;
        public double Dispersion;
    }

    public struct TracingResult
    {
        public TracingResult(List<(int x, int y)> coordinates, Color meanColor)
        {
            Coordinates = coordinates;
            MeanColor = meanColor;
        }

        public List<(int x, int y)> Coordinates;
        public Color MeanColor;
    }

    public class Tracer
    {
        private const int MaxStepLength = 20;
        private const int StepOffset = 1;
        private const int Tol = 100;
        private const int Segments = 1;

        private static (int x, int y) PointOffset((int x, int y) p, double angle, double length)
        {
            return (
                p.x + (int)(length * Math.Cos(angle)),
                p.y + (int)(length * Math.Sin(angle)));
        }

        public static TracingResult GetIterativeTracePath(ArtBitmap bitmap, (int x, int y) p1, double angle)
        {
            ROIData roi = GetROI(bitmap, p1);

            MeanColorCalculator segmentedCalc = roi.Calculator.Copy();
            List<(int x, int y)> segmentedPathCoordinates = roi.Coordinates;

            int brushStep = MaxStepLength;

            while (brushStep >= 2)
            {
                (int x, int y) p2 = PointOffset(p1, angle, brushStep);
                TracingPath path = GetPath(bitmap, p1, p2, segmentedPathCoordinates, segmentedCalc, roi.Radius);

                double currentDispersion = path.Dispersion;
                Color currentMeanColor = path.MeanColor;
                List<(int x, int y)> currentPath = path.Coordinates;
                MeanColorCalculator currentCalc = path.Calculator;

                // Дисперсия в норме
                if (currentDispersion < Tol)
                {
                    segmentedPathCoordinates.AddRange(currentPath);
                    segmentedCalc.MergeWith(currentCalc);

                    return new TracingResult(segmentedPathCoordinates, currentMeanColor);
                }
                else
                {
                    brushStep -= 1;
                }
            }

            return new TracingResult(roi.Coordinates, roi.Calculator.GetMeanColor());
        }

        private static void UpdateSegmentedCoordinates(List<(int x, int y)> segmented, List<(int x, int y)> newsegmented)
        {
            foreach (var item in newsegmented)
            {
                if (!segmented.Contains(item))
                {
                    segmented.Add(item);
                }
            }
        }

        private static TracingPath GetPath(
            ArtBitmap bitmap,
            (int x, int y) p1,
            (int x, int y) p2,
            List<(int x, int y)> segmentedPathCoordinates,
            MeanColorCalculator segmentedCalc,
            int radius)
        {

            MeanColorCalculator localCalc = segmentedCalc.Copy();

            List<(int x, int y)> localPathCoordinates = new();
            localPathCoordinates.AddRange(segmentedPathCoordinates);

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
                            localCalc.AddColor(bitmap[c.x, c.y]);
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
                for (int i = 0; i < StepOffset - 1; i++)
                {
                    if (p1.x == p2.x && p1.y == p2.y)
                        break;

                    p1.x += sx;
                    p1.y += sy;
                }
            }

            Color meanColor = localCalc.GetMeanColor();
            double dispersion = StrokeUtils.GetDispersion(bitmap, localPathCoordinates, meanColor);

            return new TracingPath()
            {
                Coordinates = localPathCoordinates,
                MeanColor = meanColor,
                Dispersion = dispersion,
                Calculator = localCalc
            };
        }

        public struct ROIData
        {
            public ROIData()
            {

            }

            public List<(int x, int y)> Coordinates;
            public MeanColorCalculator Calculator;
            public int Radius;
        }


        private const int MaxRadius = 10;

        private static ROIData GetROI(ArtBitmap bitmap, (int x, int y) p)
        {
            int x = p.x;
            int y = p.y;

            int radius = MaxRadius;
            while (radius >= 2)
            {
                CircleMaskResult circle = StrokeCircleMask.ApplyCircleMask(bitmap, x, y, radius);
                MeanColorCalculator calc = new MeanColorCalculator(bitmap, circle.Coordinates);
                double dispesion = StrokeUtils.GetDispersion(bitmap, circle.Coordinates, calc.GetMeanColor());

                if (dispesion < Tol || radius == 2)
                {
                    return new ROIData()
                    {
                        Coordinates = circle.Coordinates,
                        Calculator = calc,
                        Radius = radius
                    };
                }
                else
                {
                    radius -= 1;
                }
            }

            throw new NotImplementedException();
        }
    }
}
