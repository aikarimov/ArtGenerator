using System.Drawing;
using System.Drawing.Imaging;

namespace ArtModel.ImageModel
{
    public unsafe class ArtBitmap
    {
        private Bitmap _bitmap;
        private BitmapData _bitmapData;
        private unsafe byte* _pixelData;
        public int Width => _bitmap.Width;
        public int Height => _bitmap.Height;

        public ArtBitmap(Bitmap bitmap)
        {
            _bitmap = bitmap;
            LockBitmap();
        }

        public ArtBitmap(int width, int height)
        {
            _bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            LockBitmap();
        }

        public ArtBitmap Copy()
        {
            return new ArtBitmap(((Bitmap)_bitmap.Clone()));
        }

        public void LockBitmap()
        {
            Rectangle rect = new Rectangle(0, 0, _bitmap.Width, _bitmap.Height);
            _bitmapData = _bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            _pixelData = (byte*)_bitmapData.Scan0;
        }

        public void UnlockBitmap()
        {
            _bitmap.UnlockBits(_bitmapData);
        }

        public Color this[int x, int y]
        {
            get
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    int position = ((Height - y - 1) * _bitmapData.Stride) + (x * 3);
                    byte blue = _pixelData[position];
                    byte green = _pixelData[position + 1];
                    byte red = _pixelData[position + 2];
                    return Color.FromArgb(red, green, blue);
                }
                else
                {
                    throw new IndexOutOfRangeException("Coordinates are outside the image boundaries.");
                }
            }
            set
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    int position = ((Height - y - 1) * _bitmapData.Stride) + (x * 3);
                    _pixelData[position] = value.B;
                    _pixelData[position + 1] = value.G;
                    _pixelData[position + 2] = value.R;
                }
                else
                {
                    //throw new IndexOutOfRangeException("Coordinates are outside the image boundaries.");
                }
            }
        }

        public void Save(string outputPath, string fileName)
        {
            _bitmap.Save($"{outputPath}\\{fileName}.{ImageFormat.Png}");
        }
    }
}
