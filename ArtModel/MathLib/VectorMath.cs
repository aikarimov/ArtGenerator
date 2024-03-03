using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.MathLib
{
    public class VectorMath
    {
        public static (int x, int y) PointOffset((int x, int y) p, double angle, double length)
        {
            return (
                p.x + (int)(length * Math.Cos(angle)),
                p.y + (int)(length * Math.Sin(angle)));
        }

        public static (int x, int y) PointOffsetClamp((int x, int y) p, double angle, double length, int clampX, int clampY)
        {
            return (
                    Math.Clamp(p.x + (int)(length * Math.Cos(angle)), 0, clampX),
                    Math.Clamp(p.y + (int)(length * Math.Sin(angle)), 0, clampY));
        }
    }
}
