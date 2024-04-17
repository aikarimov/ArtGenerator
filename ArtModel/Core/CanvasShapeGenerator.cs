using ArtModel.ImageProccessing;
using MoreLinq;
using System.Drawing;

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

    public class StrokeRestoreData
    {
        public HashSet<(int y, int x)> Pixels { get; set; }

        public StrokeRestoreData()
        {
            Pixels = new();
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
        private Stack<(ushort index, bool isShape)>[,] _canvas;

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

            _canvas = new Stack<(ushort, bool)>[_height, _width];
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _canvas[y, x] = new Stack<(ushort, bool)>();
                }
            }
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

        public void AddPixel((int x, int y) point, ShapeType shapeType)
        {
            var currentShapeStack = _canvas[point.y, point.x];

            // Значит на этой точке уже есть какой-то мазок
            if (currentShapeStack.TryPeek(out var topIndex))
            {
                ShapeData shape = _shapes[topIndex.index];
                shape.CurrentSize -= 1;
            }

            // Заполняем пиксель признаком, что это новый мазок
            _canvas[point.y, point.x].Push((_currentIndex, shapeType == ShapeType.Shape));
            _currentShapeData.IntialSize += 1;
            _currentShapeData.CurrentSize += 1;
        }

        public ArtBitmap CreateShapesBitmap()
        {
            ArtBitmap shapesBm = new ArtBitmap(_width, _height);
            shapesBm.FillColor(Color.White);

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var stack = _canvas[y, x];

                    while (stack.Count > 0)
                    {
                        var stackTop = stack.Pop();
                        ushort index = stackTop.index;
                        ShapeData data = _shapes[index];

                        if (data.GetFraction() >= VisibilityFraction)
                        {
                            if (stackTop.isShape)
                            {
                                shapesBm[x, y] = data.ShapeColor;
                            }
                            else
                            {
                                shapesBm[x, y] = Color.White;
                            }
                            break;
                        }
                    }
                }
            }

            return shapesBm;
        }
    }
}
