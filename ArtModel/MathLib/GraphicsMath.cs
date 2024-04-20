using ArtModel.ImageProccessing;
using System.Drawing;

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

        public static Color CalculateAlpha(in Color back, in Color front, in double a)
        {
            return Color.FromArgb(
                Math.Clamp((int)(a * front.R + (1 - a) * back.R), 0, 255),
                Math.Clamp((int)(a * front.G + (1 - a) * back.G), 0, 255),
                Math.Clamp((int)(a * front.B + (1 - a) * back.B), 0, 255));
        }

        public static double ColorEuclideanDistance(in Color color1, in Color color2)
        {
            double R_sq = Math.Pow(color1.R - color2.R, 2);
            double G_sq = Math.Pow(color1.G - color2.G, 2);
            double B_sq = Math.Pow(color1.B - color2.B, 2);
            return Math.Sqrt(R_sq + G_sq + B_sq);
        }

        public static double CalculateSquaredEuclideanDistance(in Color color1, in Color color2)
        {
            double R_sq = Math.Pow(color1.R - color2.R, 2);
            double G_sq = Math.Pow(color1.G - color2.G, 2);
            double B_sq = Math.Pow(color1.B - color2.B, 2);
            return R_sq + G_sq + B_sq;
        }

        public static double GetDispersion(ArtBitmap bitmap, in Color meanColor, params HashSet<(int x, int y)>[] pixelSets)
        {
            double sum = 0.0;
            int count = 0;

            foreach (var set in pixelSets)
            {
                foreach (var pixel in set)
                {
                    count++;
                    double eucl = CalculateSquaredEuclideanDistance(bitmap[pixel.x, pixel.y], meanColor);
                    sum += eucl;
                }
            }

            return (sum / count);
        }
    }
}
