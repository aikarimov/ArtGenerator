using ArtModel.ImageProccessing;
using ArtModel.MathLib;
using ArtModel.Tracing.PathTracing.Shapes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.Tracing.PathTracing
{
    public struct TracingPath
    {
        public MeanColorCalculator Calculator { get; set; }
        public HashSet<(int x, int y)> Coordinates { get; set; }
        public Color MeanColor { get; set; }
        public double Dispersion { get; set; }

        // Поля для обратной связи, т.к. много путей ищутся параллельно
        public int Length { get; set; }
        public (int x, int y) EndPoint { get; set; }
        public double Angle { get; set; }
    }

    public class LinearPathTracer
    {
        public static TracingPath GetPath(
            ArtBitmap bitmap,
            (int x, int y) pointStart,
            (int x, int y) pointEnd,
            HashSet<(int x, int y)> segmentedPathCoordinates,
            MeanColorCalculator segmentedCalc,
            int width)
        {
            int radius = width / 2;

            (double x, double y) vector = (pointEnd.x - pointStart.x, pointEnd.y - pointStart.y);
            double angle = Math.Atan2(vector.y, vector.x);

            // 1-3 линия, 2-4 линия
            var p1 = VectorMath.PointOffset(pointStart, angle + Math.PI / 2, radius);
            var p2 = VectorMath.PointOffset(pointStart, angle - Math.PI / 2, radius);
            var p3 = VectorMath.PointOffset(pointEnd, angle + Math.PI / 2, radius);
            var p4 = VectorMath.PointOffset(pointEnd, angle - Math.PI / 2, radius);
            (int x, int y)[] points = [p1, p3, p4, p2]; // Порядок, чтобы шли по кругу

            IShape rect = new RectangleShape(points);

            var borders = GetOuterRectangle(points);

            HashSet<(int x, int y)> localPathCoordinates = new();
            MeanColorCalculator localCalculator = segmentedCalc.Copy();

            // Точки в прямоугольнике
            for (int x = borders.leftBottom.x; x < borders.rightTop.x; x++)
            {
                for (int y = borders.leftBottom.y; y < borders.rightTop.y; y++)
                {
                    if (rect.IsInside((x, y)) &&
                        bitmap.IsInside(x, y) &&
                        !localPathCoordinates.Contains((x, y)) &&
                        !segmentedPathCoordinates.Contains((x, y)))
                    {
                        localPathCoordinates.Add((x, y));
                        localCalculator.AddColor(bitmap[x, y]);
                    }
                }
            }

            // Точки в окружностях
            AddCirclePoints(StrokeCircleMask.ApplyCircleMask(bitmap, pointStart.x, pointStart.y, radius));
            AddCirclePoints(StrokeCircleMask.ApplyCircleMask(bitmap, pointEnd.x, pointEnd.y, radius));

            Color meanColor = localCalculator.GetMeanColor();
            double dispersion = StrokeUtils.GetDispersion(bitmap, meanColor, localPathCoordinates, segmentedPathCoordinates);

            return new TracingPath()
            {
                Coordinates = localPathCoordinates,
                MeanColor = meanColor,
                Dispersion = dispersion,
                Calculator = localCalculator,
                EndPoint = pointEnd,
            };

            void AddCirclePoints(in CircleMaskResult result)
            {
                foreach (var c in result.Coordinates)
                {
                    // Условие bitmap.IsInside выполняется внутри ApplyCircleMask, а условие rect.IsInside не нужно, т.к. его точки уже в хэшсетах
                    if (!localPathCoordinates.Contains((c.x, c.y)) &&
                        !segmentedPathCoordinates.Contains((c.x, c.y)))
                    {
                        localPathCoordinates.Add((c.x, c.y));
                        localCalculator.AddColor(bitmap[c.x, c.y]);
                    }
                }
            }
        }

        private static ((int x, int y) leftBottom, (int x, int y) rightTop) GetOuterRectangle(in (int x, int y)[] points)
        {
            int minX = points.Min(point => point.x);
            int maxX = points.Max(point => point.x);

            int minY = points.Min(point => point.y);
            int maxY = points.Max(point => point.y);

            return ((minX, minY), (maxX, maxY));
        }
    }
}
