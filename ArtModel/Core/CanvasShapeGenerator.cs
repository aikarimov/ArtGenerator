using ArtModel.ImageProccessing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.Core
{
    public class CanvasShapeGenerator
    {
        private class ShapeData
        {
            public int IntialSize { get; set; }

            public int CurrentSize { get; set; }


        }

        public CanvasShapeGenerator(ArtBitmap bitmap)
        {
            _width = bitmap.Width;
            _height = bitmap.Height;
            _canvas = new short[_width, _height];
        }
        private short[,] _canvas;

        private int _width;

        private int _height;

        private int _currentIndex = 0;

        private Color _currentColor;

        private Dictionary<int, Color> _pixels;



        public void OpenNewStroke(Color color)
        {
            _currentIndex++;
            _currentColor = color;
        }

        public void AddPixel((int x, int y) point)
        {
            int curr = _canvas[point.x, point.y];
        }

    }
}
