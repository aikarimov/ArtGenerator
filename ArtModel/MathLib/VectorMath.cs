using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ArtModel.MathLib
{
    public class VectorMath
    {
        // Отступ от точки в направлении угла на определенную длину
        public static (int x, int y) PointOffset((int x, int y) p, double angle, double length)
        {
            return (
                p.x + (int)(length * Math.Cos(angle)),
                p.y + (int)(length * Math.Sin(angle)));
        }

        // Отступ от точки в направлении угла на определенную длину (float)
        public static (float x, float y) PointOffsetF((int x, int y) p, double angle, double length)
        {
            return (
                p.x + (float)(length * Math.Cos(angle)),
                p.y + (float)(length * Math.Sin(angle)));
        }

        // Отступ от точки в направлении угла на определенную длину + отсечение, если точка вышла за рамки
        public static (int x, int y) PointOffsetClamp((int x, int y) p, double angle, double length, int clampX, int clampY)
        {
            return (
                    Math.Clamp(p.x + (int)(length * Math.Cos(angle)), 0, clampX),
                    Math.Clamp(p.y + (int)(length * Math.Sin(angle)), 0, clampY));
        }

        // Угол между двумя векторами. Через косинусное произведенеие
        public static double AngleBy2Vectors((int x, int y) v1, (int x, int y) v2)
        {
            double a1 = v1.x * v2.x + v1.y * v2.y;
            double a2 = Math.Sqrt(v1.x * v1.x + v1.y * v1.y) * Math.Sqrt(v2.x * v2.x + v2.y * v2.y);
            return Math.Acos(a1 / a2);
        }

        // Угол по трём точкам, p1 -> p2 -> p3.
        public static double AngleBy3Points((int x, int y) p1, (int x, int y) p2, (int x, int y) p3)
        {
            (int x, int y) v1 = ((p2.x - p1.x), (p2.y - p1.y));
            (int x, int y) v2 = ((p2.x - p3.x), (p2.y - p3.y));
            double angle = AngleBy2Vectors(v1, v2);
            return angle;
        }

        // Векторное произведение
        public static double VectorProduct((int x, int y) v1, (int x, int y) v2)
        {
            return (v1.x * v2.y - v1.y * v2.x);
        }

        // Если идти по точкам p1 - p2 - p3, где будет p3 относительно прошлой прямой? -1 справа, +1 слева, 0 на одной прямой
        public static int LeftOrRight((int x, int y) p1, (int x, int y) p2, (int x, int y) p3)
        {
            (int x, int y) v1 = ((p2.x - p1.x), (p2.y - p1.y));
            (int x, int y) v2 = ((p3.x - p1.x), (p3.y - p1.y));
            return Math.Sign(VectorProduct(v1, v2));
        }
    }
}
