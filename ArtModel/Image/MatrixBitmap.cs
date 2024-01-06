using ArtModel.ColorModel;
using ArtModel.ColorModel.ColorSpaces;
using ArtModel.MathLib;

namespace ArtModel.Image
{
    public class MatrixBitmap
    {
        private Matrix2D<PixelData> _matrix;

        public int Rows => _matrix.Rows;

        public int Columns => _matrix.Columns;

        public readonly int ComponentsPerPixel;

        public readonly ColorSpaceType ColorSpace;

        public PixelData this[int x, int y]
        {
            get
            {
                // Инверс
                return _matrix[y, x];
            }
            set
            {
                _matrix[y, x] = value;
            }
        }

        public MatrixBitmap(int rows, int columns, ColorSpaceType type)
        {
            ColorSpace = type;
            ComponentsPerPixel = ColorSpaceFormats.ColorSpaces[type].ComponentsCount;
            _matrix = new Matrix2D<PixelData>(rows, columns);
        }

        public MatrixBitmap(MatrixBitmap other) : this(other.Rows, other.Columns, other.ColorSpace) { }

        public void WriteImage(Matrix2D<PixelData> matrix, int x, int y)
        {
            for (int i = x; i < matrix.Columns; i++)
            {
                for (int j = y; j < matrix.Rows; j++)
                {
                    _matrix[i, j] = _matrix[i, j];
                }
            }
        }

        public void WritePixels(IEnumerable<(int x, int y, PixelData data)> pixels)
        {
            foreach (var pixel in pixels)
            {
                _matrix[pixel.x, pixel.y] = pixel.data;
            }
        }
    }
}
