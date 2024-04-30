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

        /*public HashSet<(int x, int y)> GetEdgesContour()
        {
            //return Edges.Values;


            //Edges[Edge.Right].Reverse();
            //Edges[Edge.Bottom].Reverse();
        }*/

        public HashSet<(int x, int y)> Rotate(double rotationAngle, int oldW, int oldH, int newW, int newH)
        {
            var result = new HashSet<(int x, int y)>();

            rotationAngle += 1.5 * Math.PI;

            double sin = Math.Sin(rotationAngle);
            double cos = Math.Cos(rotationAngle);

            Edges[Edge.Right].Reverse();
            Edges[Edge.Bottom].Reverse();

            foreach (var edge in Edges.Values)
            {
                foreach (var p in edge)
                {
                    var px = p.x - oldW / 2;
                    var py = p.y - oldH / 2;

                    int px_new = (int)(px * cos - py * sin);
                    int py_new = (int)(px * sin + py * cos);

                    px_new += newW / 2;
                    py_new += newH / 2;

                    result.Add((px_new, py_new));
                }
            }

            return result;
        }
    }

    public class ShapeData
    {
        public int IntialSize { get; set; }

        public int CurrentSize { get; set; }

        public Dictionary<int, (int x, int y)> PathPoints { get; set; }

        public Rectangle Bounds { get; set; }

        public Color ShapeColor { get; set; }

        public StrokeEdge Edge { get; set; }

        public double GetFraction()
        {
            return ((double)CurrentSize) / IntialSize;
        }
    }

    public enum ShapeType
    {
        Filler,
        Shape
    }

    public class CanvasShapeGenerator
    {
        // Индекс мазка, флаг того что это контур
        private HashSet<ushort>[,] _canvas;

        private int _width;

        private int _height;

        private ushort _currentIndex = 0;

        private Dictionary<int, ShapeData> _shapes;

        private ShapeData _currentShapeData;

        private const double VisibilityFraction = 0.5;

        public CanvasShapeGenerator(ArtBitmap bitmap)
        {
            _width = bitmap.Width;
            _height = bitmap.Height;
            _shapes = new();

            _canvas = new HashSet<ushort>[_height, _width];
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
            var data = _shapes[index];
            var rect = data.Bounds;
            for (int x = rect.X; x < rect.Width; x++)
            {
                for (int y = rect.Y; y < rect.Height; y++)
                {
                    if (_canvas[x, y].Contains(index))
                    {
                        _canvas[x, y].Remove(index);
                    }
                }
            }
            _shapes.Remove(index);
        }

        public void OpenNewStroke(Color color, Rectangle bounds)
        {
            _currentIndex++;

            _currentShapeData = new ShapeData()
            {
                ShapeColor = color,
                Bounds = bounds
            };

            _shapes.Add(_currentIndex, _currentShapeData);
        }

        public void AddPixel((int x, int y) point)
        {
            var pointSet = _canvas[point.y, point.x];
            ushort index = pointSet.Count > 0 ? pointSet.LastOrDefault() : (ushort)0;

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
            _canvas[point.y, point.x].Add(_currentIndex);
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
                    var set = _canvas[y, x];




                    /* while (stack.Count > 0)
                     {
                         var stackTop = stack.Pop();
                         ushort index = stackTop.index;
                         ShapeData data = _shapes[index];

                         if (data.GetFraction() >= VisibilityFraction)
                         {
                             // Контур
                             if (stackTop.isShape)
                             {
                                 shapesBm[x, y] = data.ShapeColor;
                             }
                             else
                             {
                                 shapesBm[x, y] = Color.White;
                             }

                             // Скелет
                             for (int i = 1; i < data.PathPoints.Count; i++)
                             {
                                 foreach (var p in GraphicsMath.GetLinePoints(data.PathPoints[i], data.PathPoints[i + 1]))
                                 {
                                     skeletBm[p.x, p.y] = data.ShapeColor;
                                 }
                             }

                             break;
                         }
                     }*/
                }
            }

            return (shapesBm, skeletBm);
        }
    }
}
