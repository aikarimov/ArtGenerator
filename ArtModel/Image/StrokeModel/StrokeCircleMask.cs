using ArtModel.ColorModel;
using System.Collections;
using ArtModel.Image.Matrix;

namespace ArtModel.Image.StrokeModel
{
    public struct CircleMask
    {
        public CircleMask(List<(int x, int y)> coordinates, List<PixelData> data)
        {
            Coordinates = coordinates;
            Data = data;
        }

        public List<(int x, int y)> Coordinates;
        public List<PixelData> Data;
    }

    public static class StrokeCircleMask
    {
        private static Dictionary<int, bool[,]> _masks = new();

        static StrokeCircleMask()
        {
            for (int i = 1; i < 15; i++)
            {
                GetMask(i);
            }
        }

        public static CircleMask ApplyCircleMask(MatrixBitmap matrix, int x, int y, int radius)
        {
            bool[,] mask = GetMask(radius);

            List<(int x, int y)> coordinates = new();
            List<PixelData> data = new();

            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    if (mask[radius + j, radius + i])
                    {
                        int rx = x + i;
                        int ry = y + j;

                        if (rx > 0 && rx < matrix.Width && ry > 0 && ry < matrix.Height)
                        {
                            coordinates.Add((rx, ry));
                            data.Add(matrix[rx, ry]);
                        }
                    }
                }
            }

            return new CircleMask(coordinates, data);
        }

        private static bool[,] GetMask(int radius)
        {
            if (_masks.ContainsKey(radius))
            {
                return _masks[radius];
            }
            else
            {
                var mask = CreateNewCircleMask(radius);
                _masks.Add(radius, mask);
                return mask;
            }
        }

        private static bool[,] CreateNewCircleMask(int radius)
        {
            int diameter = 2 * radius + 1;
            bool[,] mask = new bool[diameter, diameter];

            int centerX = radius;
            int centerY = radius;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    int distanceSquared = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);
                    mask[y, x] = distanceSquared <= radius * radius;
                }
            }

            return mask;
        }
    }
}
