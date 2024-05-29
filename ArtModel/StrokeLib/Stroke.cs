using ArtModel.ImageProccessing;
using ArtModel.Tracing;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ArtModel.StrokeLib
{
    public class Stroke : ArtBitmap
    {
        public static byte BLACK_BORDER_WEAK = 200;
        public static byte BLACK_BORDER_MEDIUM = 240;
        public static byte BLACK_BORDER_STRONG = 250;
        public static byte BLACK_BORDER_ABSOLUTE = 255;

        public bool IsPhongLighted = false;

        private object locker = new object();

        public Stroke? NormalMap { get; set; }
        public StrokeShape Shape { get; set; }
        public Stroke? PhongModel { get; set; }

        public Stroke(Bitmap bitmap) : base(bitmap)
        {
            SP = new StrokePropertyCollection<double>();
            PivotPoint = (Width / 2, Height / 2);
        }

        public Stroke(Stroke stroke) : base(stroke.GetBitmap())
        {
            SP = stroke.SP;
            PivotPoint = stroke.PivotPoint;
        }

        public (int x, int y) PivotPoint { get; set; }

        public StrokePropertyCollection<double> SP { get; set; }

        public new Stroke Copy()
        {
            // Трай кетч- заглушка, почему-то иногда кидает эксепшены не смотря на lock
           
            var clonedBitmap = (Bitmap)bitmap.Clone();
            return new Stroke(clonedBitmap)
            {
                SP = SP,
                PivotPoint = PivotPoint,
                NormalMap = NormalMap
            };
            try
            {
                
            }
            catch
            {
               // return new Stroke(new Bitmap(Width, Height));
            }            
        }

        public void Flip(RotateFlipType flipType)
        {
            UnlockBitmap();

            Bitmap mirroredBitmap = (Bitmap)bitmap.Clone();
            mirroredBitmap.RotateFlip(flipType);

            // bitmap.Dispose();
            bitmap = mirroredBitmap;

            LockBitmap();
        }

        public void Resize(double coefficient)
        {
            if (coefficient == 1)
                return;

            Math.Clamp(coefficient, 0.001, 100000);

            UnlockBitmap();

            Width = (int)Math.Ceiling(Width * coefficient);
            Height = (int)Math.Ceiling(Height * coefficient);

            Bitmap resizedImage = new Bitmap(Width, Height);
            resizedImage.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);

            using (Graphics g = Graphics.FromImage(resizedImage))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(bitmap, new Rectangle(0, 0, Width, Height));
            }

            bitmap.Dispose();
            bitmap = resizedImage;
            PivotPoint = (Width / 2, Height / 2);

            LockBitmap();
        }

        // Что тут происходит:
        // На вход мы подаём абсолютный угол, на который хотим повернуть мазок. В библиотеке все мазки изначально смотрят вверх, поэтому вычитаем из угла PI/2, это relAngle
        // Поворот использует RotateTransform, поэтому мы создаём новую битмапу, заранее расчитывая её размеры, учитывая угол поворота через синусы и косинусы.
        // Это будет битмапа, в которую попадёт наш исходный прямоугольник, когда его повернут на заданный угол.
        // Но поворота битмпаы недостаточно, требуется повернуть точку привязки мазка. Это точка, из которой он будет рисоваться. Она поворачивается через матрицу поворота.
        // Изначально через CalculatePivotPoint() расчитывается исходная точка привзяки в исходной системе координат
        // Далее она смещается в новую систему координат, находящейся в центре прямоугольника.
        // 
        // Я забыл как оно работает :(
        // 
        public void Rotate(double rotationAngle, Color fillColor)
        {
            double relAngle = rotationAngle - Math.PI / 2;

            double cosA = Math.Abs(Math.Cos(relAngle));
            double sinA = Math.Abs(Math.Sin(relAngle));
            int newWidth = (int)(cosA * Width + sinA * Height);
            int newHeight = (int)(cosA * Height + sinA * Width);

            var originalPivot = CalculatePivotPoint();
            originalPivot = (originalPivot.x - Width / 2, originalPivot.y - Height / 2);

            originalPivot = RotatePoint(originalPivot, rotationAngle + 1.5 * Math.PI);

            PivotPoint = (originalPivot.x + newWidth / 2, originalPivot.y + newHeight / 2);

            UnlockBitmap();
            Bitmap rotatedBitmap = new Bitmap(newWidth, newHeight);
            rotatedBitmap.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);

            using (Graphics g = Graphics.FromImage(rotatedBitmap))
            {
                using (SolidBrush brush = new SolidBrush(fillColor))
                {
                    g.FillRectangle(brush, 0, 0, newWidth, newHeight);
                }

                g.TranslateTransform(rotatedBitmap.Width / 2, rotatedBitmap.Height / 2);
                g.RotateTransform((float)(-relAngle * 180 / Math.PI));
                g.TranslateTransform(-bitmap.Width / 2, -bitmap.Height / 2);
                g.DrawImage(bitmap, new Point(0, 0));
            }

            bitmap.Dispose();
            bitmap = rotatedBitmap;

            Width = newWidth;
            Height = newHeight;
            LockBitmap();
        }

        public void InitShape()
        {
            Shape = new(this);
        }

        private (int x, int y) CalculatePivotPoint()
        {
            int width = Width;
            int x1 = 0;
            int x2 = width;

            for (int i = 0; i < width; i++)
            {
                if (this[i, 3].R <= BLACK_BORDER_MEDIUM)
                {
                    x1 = i;
                    break;
                }
            }

            for (int i = width - 1; i > 0; i--)
            {
                if (this[i, 3].R <= BLACK_BORDER_MEDIUM)
                {
                    x2 = i;
                    break;
                }
            }
            return ((x1 + x2) / 2, 0);
        }

        private (int x, int y) RotatePoint(in (int x, int y) point, in double angle)
        {
            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);

            int xnew = (int)(point.x * cos - point.y * sin);
            int ynew = (int)(point.x * sin + point.y * cos);

            return (xnew, ynew);
        }

        public (HashSet<(int x, int y)> coordinates, double dispersion) GetBitmapCoordinates(ArtBitmap bitmap, (int x, int y) globalPoint, Color strokeColor)
        {
            throw new NotImplementedException();

            MeanColorCalculator calc = new();
            HashSet<(int x, int y)> globalCoordinates = new();


            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int globalX = globalPoint.x - PivotPoint.x + x;
                    int globalY = globalPoint.y - PivotPoint.y + y;

                    byte strokeAlpha = this[x, y].R;
                    if (bitmap.IsInside(globalX, globalY) && strokeAlpha < Stroke.BLACK_BORDER_MEDIUM)
                    {
                        globalCoordinates.Add((globalX, globalY));



                        bitmap[globalX, globalY] = CalculateAlpha(bitmap[globalX, globalY], strokeColor, (255.0 - strokeAlpha) / 255.0);

                    }
                }
            }

            //double dispersion = StrokeUtils.GetDispersion(bitmap, meanColor, localPathCoordinates, segmentedPathCoordinates);

            Color CalculateAlpha(in Color back, in Color front, in double a)
            {
                return Color.FromArgb(
                    Math.Clamp((int)(a * front.R + (1 - a) * back.R), 0, 255),
                    Math.Clamp((int)(a * front.G + (1 - a) * back.G), 0, 255),
                    Math.Clamp((int)(a * front.B + (1 - a) * back.B), 0, 255));
            }
        }
    }
}
