using ArtModel.Tracing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.MathLib
{
    public class GraphicsMath
    {
        public static IEnumerable<(int x, int y)> GetLinePoints((int x, int y) p1, (int x, int y) p2)
        {
            HashSet<(int x, int y)> points = new HashSet<(int x, int y)>();

            int x1 = p1.x, y1 = p1.y;
            int x2 = p2.x, y2 = p2.y;

            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                points.Add((x1, y1));

                if (x1 == x2 && y1 == y2)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }

            return points;
        }
    }
}
