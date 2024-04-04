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

    public class OrderedTilesList : List<(int x, int y)>
    {
        public OrderedTilesList()
        {

        }

    }

    public class RegionPointDecider : IPointDecider
    {
        private ArtBitmap _original;

        private ArtBitmap _artificial;

        private Tile[,] _tiles;

        private List<(int x, int y)> _avaliableTiles;

        private List<(int x, int y)> _orderedTileIndex;

        private int _tileWidth;

        private int _tileHeight;

        private double _dispersionBound;

        public RegionPointDecider(ArtBitmap original, ArtBitmap arificial, int tileWidth, int tileHeight, double dispersionBound)
        {
            _original = original;
            _artificial = arificial;
            _tileWidth = tileWidth;
            _tileHeight = tileHeight;
            _dispersionBound = dispersionBound;

            GenerateTiles(_tileWidth, _tileHeight);
            ClearTilesAvaliables();
            RecalculateList();
        }

        private void GenerateTiles(int tileWidth, int tileHeight)
        {
            // Предрасчёт размеров массива тайлов
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
        }

        private void ClearTilesAvaliables()
        {
            int height = _tiles.GetLength(0);
            int width = _tiles.GetLength(1);
            _avaliableTiles = new();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    _avaliableTiles.Add((x, y));
                }
            }
        }

        // Переписать
        private void RecalculateList()
        {
            _orderedTileIndex = new();

            for (int y = 0; y < _tiles.GetLength(0); y++)
            {
                for (int x = 0; x < _tiles.GetLength(1); x++)
                {
                    _orderedTileIndex.Add((x, y));
                }
            }

            _orderedTileIndex.Sort((s1, s2) => _tiles[s2.y, s2.x].Dispersion.CompareTo(_tiles[s1.y, s1.x].Dispersion));
        }

        private Tile GetMaxDispersionTile()
        {
            for (int i = 0; i < _orderedTileIndex.Count; i++)
            {
                var index = _orderedTileIndex[i];
                var tile = _tiles[index.y, index.x];

                if (_avaliableTiles.Contains(index) && tile.Dispersion >= _dispersionBound)
                {
                    _avaliableTiles.Remove((index.x, index.y));
                    return _tiles[index.y, index.x];
                }
            }

            throw new Exception();
        }

        public (int x, int y) GetNewPoint()
        {
            return GetMaxDispersionTile().MaxDispersionPoint;
        }

        public bool DeciderAvaliable()
        {
            return true;
        }

        public void PointCallback((int x, int y) point)
        {

        }

        public void PostStroke()
        {
            GenerateTiles(_tileWidth, _tileHeight);
            RecalculateList();
        }
    }
}
