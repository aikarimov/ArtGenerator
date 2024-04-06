using ArtModel.ImageProccessing;
using System.Drawing;

namespace ArtModel.Tracing.PointDeciding
{
    public class Tile
    {
        public double Dispersion;
        public (int x, int y) MaxDispersionPoint;

        private (int x, int y) _p;
        private (int w, int h) _wh;

        public Tile(int x, int y, int width, int height)
        {
            _p = (x, y);
            _wh = (width, height);
        }

        public void CalculateDisperion(ArtBitmap original, ArtBitmap artificial)
        {
            Dispersion = 0;
            MaxDispersionPoint = _p;
            double maxLocalDispersion = 0;

            for (int x = _p.x; x < _p.x + _wh.w; x++)
            {
                for (int y = _p.y; y < _p.y + _wh.h; y++)
                {
                    double localDispersion = ColorEuclideanDistance(original[x, y], artificial[x, y]);
                    Dispersion += localDispersion;
                    if (localDispersion > maxLocalDispersion)
                    {
                        maxLocalDispersion = localDispersion;
                        MaxDispersionPoint = (x, y);
                    }
                }
            }
        }

        public bool IfInside((int x, int y) p)
        {
            return (
                   (p.x >= _p.x && p.x < _p.x + _wh.w) &&
                   (p.y >= _p.y && p.y < _p.y + _wh.h));
        }

        private static double ColorEuclideanDistance(in Color color1, in Color color2)
        {
            double redDiff = color1.R - color2.R;
            double greenDiff = color1.G - color2.G;
            double blueDiff = color1.B - color2.B;
            return Math.Sqrt(redDiff * redDiff + greenDiff * greenDiff + blueDiff * blueDiff);
        }
    }

    public class RegionPointDecider : IPointDecider
    {
        private ArtBitmap _original;

        private ArtBitmap _artificial;

        private Tile[,] _tiles;

        private bool[,] _avaliableTiles;
        private List<(int x, int y)> _orderedTileIndex;

        private int _tileWidth;
        private int _tileHeight;

        private double _dispersionBound;

        private HashSet<(int x, int y)> _tilesToRecalculate;

        private (int w, int h) _tilesCount;

        public RegionPointDecider(ArtBitmap original, ArtBitmap arificial, int tileWidth, int tileHeight, double dispersionBound)
        {
            _original = original;
            _artificial = arificial;
            _tileWidth = tileWidth;
            _tileHeight = tileHeight;
            _dispersionBound = dispersionBound;
            _tilesToRecalculate = new();

            GenerateTiles(_tileWidth, _tileHeight);
        }

        private void GenerateTiles(int tileWidth, int tileHeight)
        {
            // Разбитие поля на клетки
            int sizeX = (int)Math.Ceiling((double)_original.Width / tileWidth);
            int sizeY = (int)Math.Ceiling((double)_original.Height / tileHeight);
            _tiles = new Tile[sizeY, sizeX];
            int counterX = 0;
            for (int x = 0; x < _original.Width; x += tileWidth)
            {
                int counterY = 0;
                for (int y = 0; y < _original.Height; y += tileHeight)
                {
                    var tile = new Tile(x, y, Math.Min(tileWidth, _original.Width - 1 - x), Math.Min(tileHeight, _original.Height - 1 - y));
                    tile.CalculateDisperion(_original, _artificial);
                    _tiles[counterY, counterX] = tile;

                    counterY++;
                }
                counterX++;
            }
            _tilesCount = (_tiles.GetLength(1), _tiles.GetLength(0));

            // Создание упорядоченного списка полей по дисперсии
            _orderedTileIndex = new();
            for (int y = 0; y < _tilesCount.h; y++)
            {
                for (int x = 0; x < _tilesCount.w; x++)
                {
                    _orderedTileIndex.Add((x, y));
                }
            }
            SortOrderedList();

            // Создание списка доступных полей
            _avaliableTiles = new bool[sizeY, sizeX];
            for (int y = 0; y < _tilesCount.h; y++)
            {
                for (int x = 0; x < _tilesCount.w; x++)
                {
                    _avaliableTiles[y, x] = true;
                }
            }
        }

        private void SortOrderedList()
        {
            _orderedTileIndex.Sort((s1, s2) => _tiles[s2.y, s2.x].Dispersion.CompareTo(_tiles[s1.y, s1.x].Dispersion));
        }

        public (int x, int y) GetNewPoint()
        {
            for (int i = 0; i < _orderedTileIndex.Count; i++)
            {
                var index = _orderedTileIndex[i];
                Tile tile = _tiles[index.y, index.x];

                // Увеличить границу дисперсии. По квадратам она явно больше, чем для мазков
                if (_avaliableTiles[index.y, index.x] && tile.Dispersion >= _dispersionBound)
                {
                    _avaliableTiles[index.y, index.x] = false;
                    _orderedTileIndex.Remove(index);

                    return _tiles[index.y, index.x].MaxDispersionPoint;
                }
            }

            throw new Exception();
        }

        public bool DeciderAvaliable()
        {
            return true;
        }

        public void PointCallback((int x, int y) point)
        {
            double fractionX = (double)point.x / (_tilesCount.w * _tileWidth);
            double fractionY = (double)point.y / (_tilesCount.h * _tileHeight);

            int x_new = (int)(Math.Floor(fractionX * (_tilesCount.w)));
            int y_new = (int)(Math.Floor(fractionY * (_tilesCount.h)));

            _tilesToRecalculate.Add((x_new, y_new));
        }

        public void PostStroke()
        {
            foreach (var t in _tilesToRecalculate)
            {
                if (_avaliableTiles[t.y, t.x])
                {
                    var tile = _tiles[t.y, t.x];
                    tile.CalculateDisperion(_original, _artificial);
                    SortOrderedList();
                }
            }
            _tilesToRecalculate = new();
        }
    }
}
