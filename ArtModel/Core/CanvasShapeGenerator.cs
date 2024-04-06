using ArtModel.ImageProccessing;
using ArtModel.Tracing.PathTracing.Shapes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.Core
{
    public class ShapeData
    {
        public int IntialSize { get; set; }

        public int CurrentSize { get; set; }

        Dictionary<int, (int x, int y)> PathPoints { get; set; }

        public Color ShapeColor { get; set; }

        public double GetFraction()
        {
            return ((double)CurrentSize) / IntialSize;
        }
    }

    public class CanvasShapeGenerator
    {
        private int[,] _canvas;

        private int _width;

        private int _height;

        private int _currentIndex = 0;

        private Dictionary<int, ShapeData> _shapes;

        private ShapeData _currentShapeData;

        public CanvasShapeGenerator(ArtBitmap bitmap)
        {
            _width = bitmap.Width;
            _height = bitmap.Height;
            _canvas = new int[_height, _width];
            _shapes = new();
        }

        public void OpenNewStroke(Color color)
        {
            _currentIndex++;

            _currentShapeData = new ShapeData()
            {
                ShapeColor = color
            };

            _shapes.Add(_currentIndex, _currentShapeData);
        }

        public void AddPixel((int x, int y) point)
        {
            int currentShape = _canvas[point.y, point.x];

            if (currentShape > 0)
            {
                ShapeData shape = _shapes[currentShape];
                shape.CurrentSize -= 1;                
            }

            int curr = _canvas[point.x, point.y];
        }

    }
}
