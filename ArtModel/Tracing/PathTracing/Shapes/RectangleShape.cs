using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.Tracing.PathTracing.Shapes
{
    public class RectangleShape : IShape
    {
        private List<Func<(int x, int y), bool>> _constraints = new();

        // Линии: 0-1 1-2 2-3 3-0
        public RectangleShape((int x, int y)[] points)
        {
            var p1 = points[0];
            var p2 = points[1];
            var p3 = points[2];
            var p4 = points[3];

            bool allUniqueX = (p1.x != p2.x && p2.x != p3.x);
            bool allUniqueY = (p1.y != p2.y && p2.y != p3.y);

            if (allUniqueX && allUniqueY)
            {
                RectangleShapeConstraintsCalculator.CalculateLinearConstraints(points, _constraints);
            }
        }

        public bool IsInside((int x, int y) point)
        {
            return _constraints.Count == 0 ? true : _constraints.All(func => func(point));
        }
    }

    file class RectangleShapeConstraintsCalculator
    {
        // Ограничения - вертикальные линии. Точки на вход - с параллельных линий
        public static void CalculateVertivalConstraints((int x, int y) p1, (int x, int y) p2, List<Func<(int x, int y), bool>> constraints)
        {
            // Нам нужно чтобы p1.x было меньше p2.x
            if (p1.x > p2.x)
                (p1, p2) = (p2, p1);

            constraints.Add(p => p.x >= p1.x);
            constraints.Add(p => p.x <= p2.x);
        }

        // Ограничения - горизонтальные линии. Точки на вход - с параллельных линий
        public static void CalculateHorizontalConstraints((int x, int y) p1, (int x, int y) p2, List<Func<(int x, int y), bool>> constraints)
        {
            // Нам нужно чтобы p1.y было меньше p2.y
            if (p1.y > p2.y)
                (p1, p2) = (p2, p1);

            constraints.Add(p => p.y >= p1.y);
            constraints.Add(p => p.y <= p2.y);
        }

        // Ограничения - наклонные линии.
        public static void CalculateLinearConstraints(in (int x, int y)[] points, List<Func<(int x, int y), bool>> constraints)
        {
            var p1 = points[0];
            var p2 = points[1];
            var p3 = points[2];
            var p4 = points[3];

            // Первая пара параллельных прямых
            var line1 = GetLineCoefs(p1, p2);
            var line2 = GetLineCoefs(p3, p4);

            // Нам нужно чтобы line1.b было меньше line2.b
            if (line1.b > line2.b)
            {
                (line1, line2) = (line2, line1);
            }

            constraints.Add(p => p.y >= GetLineY(p.x, line1.k, line1.b));
            constraints.Add(p => p.y <= GetLineY(p.x, line2.k, line2.b));


            // Вторая пара параллельных прямых
            var line3 = GetLineCoefs(p2, p3);
            var line4 = GetLineCoefs(p1, p4);

            // Нам нужно чтобы line3.b было меньше line4.b
            if (line3.b > line4.b)
            {
                (line3, line4) = (line4, line3);
            }

            constraints.Add(p => p.y >= GetLineY(p.x, line3.k, line3.b));
            constraints.Add(p => p.y <= GetLineY(p.x, line4.k, line4.b));

            double GetLineY(in double x, in double k, in double b)
            {
                return k * x + b;
            }

            (double k, double b) GetLineCoefs(in (int x, int y) pp1, in (int x, int y) pp2)
            {
                double k = (pp2.y - pp1.y) * 1.0 / (pp2.x - pp1.x);
                double b = pp1.y - k * pp1.x;
                return (k, b);
            }
        }
    }
}
