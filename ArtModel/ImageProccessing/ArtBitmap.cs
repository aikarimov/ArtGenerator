using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace ArtModel.ImageProccessing
{
    public unsafe class ArtBitmap
    {
        protected Bitmap bitmap;

        private BitmapData _bitmapData;

        private unsafe byte* _pixelData;

        public int Width;

        public int Height;

        public ArtBitmap(Bitmap bitmap)
        {
            this.bitmap = bitmap;
            Width = this.bitmap.Width;
            Height = this.bitmap.Height;
            LockBitmap();
        }

        public ArtBitmap(int width, int height)
        {
            bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            Width = width;
            Height = height;
            LockBitmap();
        }

        public bool IsInside(int x, int y)
        {
            return (x >= 0 && x < Width && y >= 0 && y < Height);
        }

        public void MakeTransparent()
        {
            UnlockBitmap();
            bitmap.MakeTransparent();
            LockBitmap();
        }

        public ArtBitmap FillColor(Color color)
        {
            UnlockBitmap();
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                using (SolidBrush brush = new SolidBrush(color))
                {
                    g.FillRectangle(brush, 0, 0, Width, Height);
                }
            }
            LockBitmap();
            return this;
        }

        public ArtBitmap Copy()
        {
            ArtBitmap artBitmap = new ArtBitmap((Bitmap)bitmap.Clone());
            Width = bitmap.Width;
            Height = bitmap.Height;

            return artBitmap;
        }

        public void LockBitmap()
        {
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            _bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            _pixelData = (byte*)_bitmapData.Scan0;
        }

        public void UnlockBitmap()
        {
            bitmap.UnlockBits(_bitmapData);
        }

        public Color this[int x, int y]
        {
            get
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    int position = (Height - y - 1) * _bitmapData.Stride + x * 3;
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
                    int position = (Height - y - 1) * _bitmapData.Stride + x * 3;
                    _pixelData[position] = value.B;
                    _pixelData[position + 1] = value.G;
                    _pixelData[position + 2] = value.R;
                }
            }
        }

        public void Save(string outputPath, string fileName)
        {
            try
            {
                bitmap.Save($"{outputPath}\\{fileName}.{ImageFormat.Png}");
            }
            catch
            {
                Debug.WriteLine($"Error writing {fileName} to file {outputPath}");
            }
        }

        public Bitmap GetBitmap() { return bitmap; }
    }
}
