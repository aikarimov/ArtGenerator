using ArtModel.ImageProccessing;
using ArtModel.MathLib;
using ArtModel.Statistics;
using System.Diagnostics.Metrics;
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
                    double localDispersion = GraphicsMath.CalculateSquaredEuclideanDistance(original[x, y], artificial[x, y]);
                    Dispersion += (localDispersion);
                    if (localDispersion > maxLocalDispersion)
                    {
                        maxLocalDispersion = localDispersion;
                        MaxDispersionPoint = (x, y);
                    }
                }
            }

            if (ArtStatistics.Instance.CollectStatistics) { ArtStatistics.Instance.AddTileDispersion(Dispersion, _wh.w, _wh.h); }
        }

        public bool IfInside((int x, int y) p)
        {
            return (
                   (p.x >= _p.x && p.x < _p.x + _wh.w) &&
                   (p.y >= _p.y && p.y < _p.y + _wh.h));
        }
    }

    public class RegionPointDecider : IPointDecider
    {
        protected ArtBitmap _original;

        protected ArtBitmap _artificial;

        protected Tile[,] _tiles;

        protected bool[,] _avaliableTiles;
        protected List<(int x, int y)> _orderedTileIndex;

        protected int _tileWidth;
        protected int _tileHeight;

        protected double _dispersionMinBound;

        protected HashSet<(int x, int y)> _tilesToRecalculate;

        protected (int w, int h) _tilesCount;

        public RegionPointDecider(ArtBitmap original, ArtBitmap arificial, int tileWidth, int tileHeight, double dispersionBound)
        {
            _original = original;
            _artificial = arificial;
            _tileWidth = tileWidth;
            _tileHeight = tileHeight;
            _dispersionMinBound = dispersionBound;
            _tilesToRecalculate = new();

            GenerateTiles(_tileWidth, _tileHeight);
        }

        protected void GenerateTiles(int tileWidth, int tileHeight)
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

        protected void SortOrderedList()
        {
            _orderedTileIndex.Sort((s1, s2) => _tiles[s2.y, s2.x].Dispersion.CompareTo(_tiles[s1.y, s1.x].Dispersion));
        }

        public virtual (int x, int y) GetNewPoint()
        {
            for (int i = 0; i < _orderedTileIndex.Count; i++)
            {
                var index = _orderedTileIndex[i];
                Tile tile = _tiles[index.y, index.x];

                if (_avaliableTiles[index.y, index.x] && tile.Dispersion >= _dispersionMinBound)
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

        public virtual void PostStroke()
        {
            foreach (var t in _tilesToRecalculate)
            {
                if (_avaliableTiles[t.y, t.x])
                {
                    var tile = _tiles[t.y, t.x];
                    tile.CalculateDisperion(_original, _artificial);
                }
            }
            SortOrderedList();
            _tilesToRecalculate = new();
        }
    }

    // Модификации
    public class WeightedRegionPointDecider : RegionPointDecider
    {
        private double _boundFactor = 0.05;

        private double _dispersionBoundReuse = 0.95;

        private byte _maxReuseCount = 5;

        protected byte[,] _reuseLeft;

        public WeightedRegionPointDecider(ArtBitmap original, ArtBitmap arificial, int tileWidth, int tileHeight, double dispersionBound) : base(original, arificial, tileWidth, tileHeight, dispersionBound)
        {
            _reuseLeft = new byte[_avaliableTiles.GetLength(0), _avaliableTiles.GetLength(1)];

            double minDisp = int.MaxValue;
            double maxDisp = int.MinValue;

            for (int x = 0; x < _tilesCount.w; x++)
            {
                for (int y = 0; y < _tilesCount.h; y++)
                {
                    var disp = _tiles[y, x].Dispersion;
                    if (disp < minDisp) minDisp = disp;
                    if (disp > maxDisp) maxDisp = disp;

                    _reuseLeft[y, x] = _maxReuseCount;
                }
            }

            _dispersionMinBound = _boundFactor * (maxDisp - minDisp) + minDisp;

            _dispersionBoundReuse = _dispersionBoundReuse * (maxDisp - minDisp) + minDisp;
        }

        public override (int x, int y) GetNewPoint()
        {
            return base.GetNewPoint();


            for (int i = 0; i < _orderedTileIndex.Count; i++)
            {
                var index = _orderedTileIndex[i];
                Tile tile = _tiles[index.y, index.x];

                if (_reuseLeft[index.y, index.x] > 0 && tile.Dispersion >= _dispersionMinBound)
                {
                    if (tile.Dispersion < _dispersionBoundReuse)
                    {
                        _avaliableTiles[index.y, index.x] = false;
                        _orderedTileIndex.Remove(index);
                    }

                    return _tiles[index.y, index.x].MaxDispersionPoint;
                }
            }

            throw new Exception();
        }

        public override void PostStroke()
        {
            base.PostStroke();
            return;

            foreach (var t in _tilesToRecalculate)
            {
                if (_avaliableTiles[t.y, t.x])
                {
                    var tile = _tiles[t.y, t.x];
                    tile.CalculateDisperion(_original, _artificial);
                }
            }
            SortOrderedList();
            _tilesToRecalculate = new();
        }
    }

    public class MaxDispersionPointDecider : IPointDecider
    {
        private HashSet<(int x, int y)> _avaliableTiles = new();

        protected ArtBitmap _original;
        protected ArtBitmap _artificial;

        public MaxDispersionPointDecider(ArtBitmap original, ArtBitmap arificial)
        {
            _original = original;
            _artificial = arificial;

            for (int x = 0; x < original.Width; x += 1)
            {
                for (int y = 0; y < original.Height; y += 1)
                {
                    _avaliableTiles.Add((x, y));
                }
            }

            RecalculatePoints();
        }

        private void RecalculatePoints()
        {
            _avaliableTiles = _avaliableTiles.OrderByDescending(t => GraphicsMath.CalculateSquaredEuclideanDistance(_original[t.x, t.y], _artificial[t.x, t.y])).ToHashSet();
        }

        public bool DeciderAvaliable()
        {
            return true;
        }

        public (int x, int y) GetNewPoint()
        {
            if (_avaliableTiles.Count == 0)
                throw new Exception();

            RecalculatePoints();
            var res = _avaliableTiles.ElementAt(0);

            _avaliableTiles.Remove(res);

            return res;
        }

        public void PointCallback((int x, int y) point)
        {
            _avaliableTiles.Remove(point);
        }

        public void PostStroke()
        {
        }
    }
}
