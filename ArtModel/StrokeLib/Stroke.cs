using ArtModel.ImageProccessing;
using ArtModel.MathLib;
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

        private static Color ShapeColor = Color.FromArgb(255, 255, 0, 0);
        private static Color FillerColor = Color.FromArgb(255, 0, 255, 0);

        private object locker = new object();

        public Stroke(Bitmap bitmap) : base(bitmap)
        {
            SP = new StrokePropertyCollection<double>();
            PivotPoint = (Width / 2, Height / 2);
        }

        public (int x, int y) PivotPoint { get; private set; }

        public StrokePropertyCollection<double> SP { get; private set; }

        public new Stroke Copy()
        {
            lock (locker)
            {
                return new Stroke((Bitmap)bitmap.Clone())
                {
                    SP = SP,
                    PivotPoint = PivotPoint,
                };
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

            if (SP.GetP(StrokeProperty.Points) >= 2)
            {
                double pivotAngle = GetPivotAngle(originalPivot, (0, 0));
                double pivotRel = pivotAngle + Math.PI / 2;
                double pivotAbs = rotationAngle + Math.PI + pivotRel;
                originalPivot = RotatePoint(originalPivot, pivotAbs - pivotAngle);
            }

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

            (int x, int y) CalculatePivotPoint()
            {
                StartPointAlign align = SP.GetP(StrokeProperty.Points) == 1 ? StartPointAlign.Center : StartPointAlign.Bottom;

                if (align == StartPointAlign.Center)
                {
                    return (Width / 2, Height / 2);
                }
                else
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
            }

            double GetPivotAngle(in (int x, int y) point, in (int x, int y) center)
            {
                (int x, int y) vect = (point.x - center.x, point.y - center.y);
                return Math.Atan2(vect.y, vect.x);
            }

            (int x, int y) RotatePoint(in (int x, int y) point, in double angle)
            {
                double sin = Math.Sin(angle);
                double cos = Math.Cos(angle);

                int xnew = (int)(point.x * cos - point.y * sin);
                int ynew = (int)(point.x * sin + point.y * cos);

                return (xnew, ynew);
            }
        }

        public Stroke GetShape()
        {
            var bitmap = new ArtBitmap(Width, Height);

            int x1_prev = 0;
            int x2_prev = 0;
            // Горизонталь
            for (int y = 0; y < Height; y++)
            {

                byte shape_found = 0;

                int x1 = 0;
                int x2 = 0;

                // Скан слева
                for (int x = 0; x < Width; x++)
                {
                    if (this[x, y].R < Stroke.BLACK_BORDER_STRONG)
                    {
                        shape_found += 1;
                        x1 = x;
                        break;
                    }
                }

                // Скан справа
                for (int x = Width - 1; x >= 0; x--)
                {
                    if (this[x, y].R < Stroke.BLACK_BORDER_STRONG)
                    {
                        shape_found += 1;
                        x2 = x;
                        break;
                    }
                }

                if (shape_found < 2)
                {
                    if (x1_prev != 0 && x2_prev != 0)
                    {
                        foreach (var p in GraphicsMath.GetLinePoints((x1_prev, y), (x2_prev, y)))
                        {
                            bitmap[p.x, p.y] = ShapeColor;
                        }
                        break;
                    }
                    continue;
                }

                // Нижний и верхний уровени
                if (y == 0 || y == Height - 1 || (x1_prev == 0 && x2_prev == 0))
                {
                    foreach (var p in GraphicsMath.GetLinePoints((x1, y), (x2, y)))
                    {
                        bitmap[p.x, p.y] = ShapeColor;
                    }
                }
                //Обычная грань
                else
                {
                    x1 = (x1 + x1_prev) / 2;
                    x2 = (x2 + x2_prev) / 2;

                    for (int x_i = x1; x_i <= x2; x_i++)
                    {
                        bitmap[x_i, y] = FillerColor;
                    }

                    foreach (var p in GraphicsMath.GetLinePoints((x1_prev, y - 1), (x1, y)))
                    {
                        bitmap[p.x, p.y] = ShapeColor;
                    }

                    foreach (var p in GraphicsMath.GetLinePoints((x2_prev, y - 1), (x2, y)))
                    {
                        bitmap[p.x, p.y] = ShapeColor;
                    }
                }

                x1_prev = x1;
                x2_prev = x2;
            }

            int y1_prev = 0;
            int y2_prev = 0;
            // Вертикаль
            for (int x = 0; x < Width; x++)
            {
                byte shape_found = 0;

                int y1 = 0;
                int y2 = 0;

                // Скан снизу
                for (int y = 0; y < Height; y++)
                {
                    if (bitmap[x, y].R == 255 || bitmap[x, y].G == 255)
                    {
                        shape_found += 1;
                        y1 = y;
                        break;
                    }
                }

                // Скан сверху
                for (int y = Height - 1; y >= 0; y--)
                {
                    if (bitmap[x, y].R == 255 || bitmap[x, y].G == 255)
                    {
                        shape_found += 1;
                        y2 = y;
                        break;
                    }
                }

                if (shape_found == 0)
                {
                    if (y1_prev != 0 && y2_prev != 0)
                    {
                        foreach (var p in GraphicsMath.GetLinePoints((x, y1_prev), (x, y2_prev)))
                        {
                            bitmap[p.x, p.y] = ShapeColor;
                        }
                        break;
                    }
                    continue;
                }

                // Нижний и верхний уровени
                if (x == 0 || x == Width - 1 || (y1_prev == 0 && y2_prev == 0))
                {
                    foreach (var p in GraphicsMath.GetLinePoints((x, y1), (x, y2)))
                    {
                        bitmap[p.x, p.y] = ShapeColor;
                    }
                }

                //Обычная грань
                else
                {
                    for (int y_i = y1; y_i <= y2; y_i++)
                    {
                        bitmap[x, y_i] = FillerColor;
                    }

                    foreach (var p in GraphicsMath.GetLinePoints((x - 1, y1_prev), (x, y1)))
                    {
                        bitmap[p.x, p.y] = ShapeColor;
                    }

                    foreach (var p in GraphicsMath.GetLinePoints((x - 1, y2_prev), (x, y2)))
                    {
                        bitmap[p.x, p.y] = ShapeColor;
                    }
                }

                y1_prev = y1;
                y2_prev = y2;
            }

            bitmap.UnlockBitmap();
            Stroke result = new Stroke(bitmap.GetBitmap())
            {
                SP = this.SP,
                PivotPoint = this.PivotPoint
            };
            return result;
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

    public enum StartPointAlign
    {
        Center = 0,
        Bottom = 1,
    }
}
