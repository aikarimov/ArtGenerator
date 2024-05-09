using ArtModel.ImageProccessing;
using ArtModel.MathLib;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.XPath;

namespace ArtModel.Core
{
    public class StrokeEdge
    {
        public enum Edge
        {
            Left,
            Right,
            Top,
            Bottom
        }

        public Dictionary<Edge, HashSet<(int x, int y)>> Edges = new();

        public StrokeEdge()
        {
            Edges[Edge.Left] = new HashSet<(int x, int y)>();
            Edges[Edge.Top] = new HashSet<(int x, int y)>();
            Edges[Edge.Right] = new HashSet<(int x, int y)>();
            Edges[Edge.Bottom] = new HashSet<(int x, int y)>();
        }
    }

    public class ShapeData
    {
        public int IntialSize { get; set; }

        public int CurrentSize { get; set; }

        public Dictionary<int, (int x, int y)> PathPoints { get; set; }

        public Rectangle Bounds { get; set; }

        public Color ShapeColor { get; set; }

        public double GetFraction()
        {
            return ((double)CurrentSize) / IntialSize;
        }
    }

    public enum ShapeType
    {
        Filler,
        Edge
    }

    public class CanvasShapeGenerator
    {
        // Индекс мазка, флаг того что это контур
        private List<(ushort index, bool isEdge)>[,] _canvas;

        private int _width;

        private int _height;

        private ushort _currentIndex = 0;

        private Dictionary<int, ShapeData> _shapes;

        private ShapeData _currentShapeData;

        private const double VisibilityFraction = 0.03;

        public CanvasShapeGenerator(ArtBitmap bitmap)
        {
            _width = bitmap.Width;
            _height = bitmap.Height;
            _shapes = new();

            _canvas = new List<(ushort, bool)>[_height, _width];
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _canvas[y, x] = new();
                }
            }
        }

        private void ClearShapeFromCanvas(ushort index)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    foreach (var tuple in _canvas[y, x])
                    {
                        if (tuple.index == index)
                        {
                            _canvas[y, x].Remove(tuple);
                            break;
                        }
                    }
                }
            }
            _shapes.Remove(index);
        }

        public void OpenNewStroke(Color color)
        {
            _currentIndex++;

            _currentShapeData = new ShapeData()
            {
                ShapeColor = color,
            };

            _shapes.Add(_currentIndex, _currentShapeData);
        }

        public void AddPixel((int x, int y) point, ShapeType shapeType)
        {
            var pointSet = _canvas[point.y, point.x];
            ushort index = pointSet.Count > 0 ? pointSet.LastOrDefault().index : (ushort)0;

            // Значит на этой точке уже есть какой-то мазок
            if (index != 0)
            {
                ShapeData shape = _shapes[index];
                shape.CurrentSize -= 1;

                if (shape.GetFraction() < VisibilityFraction)
                {
                    ClearShapeFromCanvas(index);
                }
            }

            // Заполняем пиксель признаком, что это новый мазок
            _canvas[point.y, point.x].Add((_currentIndex, shapeType == ShapeType.Edge));
            _currentShapeData.IntialSize += 1;
            _currentShapeData.CurrentSize += 1;
        }

        public void AddStrokeSkelet(Dictionary<int, (int x, int y)> pathPoints)
        {
            _currentShapeData.PathPoints = pathPoints;
        }

        public (ArtBitmap shapes, ArtBitmap skelet) CreateShapesBitmap()
        {
            ArtBitmap shapesBm = new ArtBitmap(_width, _height);
            shapesBm.FillColor(Color.White);

            ArtBitmap skeletBm = new ArtBitmap(_width, _height);
            skeletBm.FillColor(Color.White);

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    foreach (var pixel in _canvas[y, x])
                    {
                        ushort index = pixel.index;
                        var isEdge = pixel.isEdge;
                        ShapeData data = _shapes[index];

                        if (isEdge)
                        {
                            shapesBm[x, y] = data.ShapeColor;
                        }
                        else
                        {
                            shapesBm[x, y] = Color.White;
                        }
                    }
                }
            }

            return (shapesBm, skeletBm);
        }
    }
}
