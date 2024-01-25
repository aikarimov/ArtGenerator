using ArtModel.ColorModel;
using ArtModel.ColorModel.ColorSpaces;
using ArtModel.MathLib;

namespace ArtModel.Image.Matrix
{
    public class MatrixBitmap
    {
        public Matrix2D<PixelData> Matrix { get; private set; }

        public int Width => Matrix.Columns;

        public int Height => Matrix.Rows;

        public byte PixelFormat => PixelData.Components;

        public ColorSpaceType ColorSpaceType;

        public PixelData this[int x, int y]
        {
            // Инверсы
            get
            {
                return Matrix[y, x];
            }
            set
            {
                Matrix[y, x] = value;
            }
        }

        public MatrixBitmap(int width, int height, ColorSpaceType type)
        {
            ColorSpaceType = type;
            ColorSpaceBase ColorSpace = ColorSpaceFormats.ColorSpaces[type];
            Matrix = new Matrix2D<PixelData>(height, width);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Matrix[y, x] = new PixelData(/*ColorSpace.ComponentsCount*/[255, 255, 255]);
                }
            }
        }

        public MatrixBitmap(MatrixBitmap other) : this(other.Width, other.Height, other.ColorSpaceType) { }

        // Converters
        public MatrixBitmap ToRgb()
        {
            return ConvertToColorSpace(ColorSpaceType.RGB, ColorSpaceFormats.ColorSpaces[ColorSpaceType].ToRGB);
        }

        public MatrixBitmap FromRgb(ColorSpaceType newType)
        {
            return ConvertToColorSpace(newType, ColorSpaceFormats.ColorSpaces[newType].FromRGB);
        }

        public MatrixBitmap ToGrayscale()
        {
            return ConvertToColorSpace(ColorSpaceType.GRAYSCALE, ColorSpaceFormats.ColorSpaces[ColorSpaceType].ToGrayscale);
        }

        delegate PixelData PixelDataConverter(PixelData pixel);

        private MatrixBitmap ConvertToColorSpace(ColorSpaceType newType, PixelDataConverter pixelDataConverter)
        {
            if (this.ColorSpaceType == newType)
            {
                return this;
            }

            MatrixBitmap matrix = new MatrixBitmap(Width, Height, newType);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    matrix[x, y] = pixelDataConverter(this[x, y]);
                }
            }

            return matrix;
        }
    }
}
