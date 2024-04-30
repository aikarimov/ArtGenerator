using ArtModel.Core;
using ArtModel.ImageProccessing;
using ArtModel.MathLib;
using System.Drawing;

namespace ArtModel.StrokeLib
{
    public class StrokeShape
    {
        private static Color FillerColor = Color.FromArgb(255, 255, 255, 255);

        private Stroke shape;
        private Stroke sourceStroke;

        private StrokeEdge edge;

        private int Width;
        private int Height;

        private Bitmap bitmap;

        public StrokeShape(Stroke stroke)
        {
            sourceStroke = stroke;
            Width = stroke.Width;
            Height = stroke.Height;
            edge = new StrokeEdge();
        }

        public Stroke GetShape()
        {
            return shape;
        }

        public new void Rotate(double rotationAngle, Color fillColor)
        {
            double relAngle = rotationAngle - Math.PI / 2;
            double cosA = Math.Abs(Math.Cos(relAngle));
            double sinA = Math.Abs(Math.Sin(relAngle));
            int newWidth = (int)(cosA * Width + sinA * Height);
            int newHeight = (int)(cosA * Height + sinA * Width);

            var rotatedSet = new HashSet<(int x, int y)>();

            double angle = rotationAngle + 1.5 * Math.PI;
            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);

            foreach (var edge in edge.Edges.Values)
            {
                foreach ((int x, int y) p in edge)
                {
                    (int x, int y) p_offset = (p.x - Width / 2, p.y - Height / 2);
                    p_offset = RotatePoint(p_offset);
                    rotatedSet.Add((p_offset.x + newWidth / 2, p_offset.y + newHeight / 2));
                }
            }

            shape.UnlockBitmap();
            var sourceBitmap = shape.GetBitmap();
            ArtBitmap rotatedBitmap = new ArtBitmap(newWidth, newHeight);
            rotatedBitmap.UnlockBitmap();
            rotatedBitmap.GetBitmap().SetResolution(sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);

            using (Graphics g = Graphics.FromImage(rotatedBitmap.GetBitmap()))
            {
                using (SolidBrush brush = new SolidBrush(fillColor))
                {
                    g.FillRectangle(brush, 0, 0, newWidth, newHeight);
                }

                g.TranslateTransform(rotatedBitmap.Width / 2, rotatedBitmap.Height / 2);
                g.RotateTransform((float)(-relAngle * 180 / Math.PI));
                g.TranslateTransform(-sourceBitmap.Width / 2, -sourceBitmap.Height / 2);
                g.DrawImage(sourceBitmap, new Point(0, 0));
            }

            sourceBitmap.Dispose();

            ArtBitmap newMap = new ArtBitmap(newWidth, newHeight);
            newMap.FillColor(Color.Black);

            shape = new Stroke(rotatedBitmap.GetBitmap());

            Width = newWidth;
            Height = newHeight;

            foreach (var p in rotatedSet)
            {
                shape[p.x, p.y] = Color.Red;
            }

            //shape.LockBitmap();


            (int x, int y) RotatePoint(in (int x, int y) point)
            {
                int xnew = (int)(point.x * cos - point.y * sin);
                int ynew = (int)(point.x * sin + point.y * cos);

                return (xnew, ynew);
            }
        }

        public void CalculateShape()
        {
            var artBitmap = new ArtBitmap(Width, Height);
            artBitmap.FillColor(Color.Black);

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
                    if (sourceStroke[x, y].R < Stroke.BLACK_BORDER_MEDIUM)
                    {
                        shape_found += 1;
                        x1 = x;
                        break;
                    }
                }

                // Скан справа
                for (int x = Width - 1; x >= 0; x--)
                {
                    if (sourceStroke[x, y].R < Stroke.BLACK_BORDER_MEDIUM)
                    {
                        shape_found += 1;
                        x2 = x;
                        break;
                    }
                }

                if (shape_found < 2)
                {
                    // Значит мы над мазком раньше чем планировалось
                    if (x1_prev != 0 && x2_prev != 0)
                    {
                        foreach (var p in GraphicsMath.GetLinePoints((x1_prev, y), (x2_prev, y)))
                        {
                            artBitmap[p.x, p.y] = FillerColor;
                        }
                        break;
                    }

                    // Ждём, пока не будет  найдена первая пара иксов, слево и спрао
                    continue;
                }

                // Нижний 
                if (x1_prev == 0 && x2_prev == 0)
                {
                    x1 = (x1 + x2) / 2;
                    x2 = x1;
                    artBitmap[x1, y] = FillerColor;
                }

                // Верхний
                else
                if (y == Height - 1)
                {
                    x1 = (x1 + x2) / 2;
                    x2 = x1;
                    artBitmap[x1, y] = FillerColor;
                }

                //Обычная грань
                else
                {
                    x1 = (x1 + x1_prev) / 2;
                    x2 = (x2 + x2_prev) / 2;

                    for (int x_i = x1; x_i <= x2; x_i++)
                    {
                        artBitmap[x_i, y] = FillerColor;
                    }

                    foreach (var p in GraphicsMath.GetLinePoints((x1_prev, y - 1), (x1, y)))
                    {
                        //bitmap[p.x, p.y] = FillerColor;
                    }

                    foreach (var p in GraphicsMath.GetLinePoints((x2_prev, y - 1), (x2, y)))
                    {
                        //bitmap[p.x, p.y] = FillerColor;
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
                    if (artBitmap[x, y].G == 255)
                    {
                        shape_found += 1;
                        y1 = y;
                        break;
                    }
                }

                // Скан сверху
                for (int y = Height - 1; y >= 0; y--)
                {
                    if (artBitmap[x, y].G == 255)
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
                            edge.Edges[StrokeEdge.Edge.Right].Add(p);
                           // bitmap[p.x, p.y] = Color.Red;
                        }
                        break;
                    }
                    continue;
                }

                // Левый 
                if (y1_prev == 0 && y2_prev == 0)
                {
                    foreach (var p in GraphicsMath.GetLinePoints((x, y1), (x, y2)))
                    {
                        edge.Edges[StrokeEdge.Edge.Left].Add(p);
                       // bitmap[p.x, p.y] = Color.Red;
                    }
                }

                // Правый
                else
                if (x == Width - 1)
                {
                    foreach (var p in GraphicsMath.GetLinePoints((x, y1), (x, y2)))
                    {
                        edge.Edges[StrokeEdge.Edge.Right].Add(p);
                        //bitmap[p.x, p.y] = Color.Red;
                    }
                }

                //Обычная грань
                else
                {
                    //y1 = (y1 + y1_prev) / 2;
                    //y2 = (y2 + y2_prev) / 2;

                    for (int y_i = y1; y_i <= y2; y_i++)
                    {
                        artBitmap[x, y_i] = FillerColor;
                    }

                    foreach (var p in GraphicsMath.GetLinePoints((x - 1, y1_prev), (x, y1)))
                    {
                        edge.Edges[StrokeEdge.Edge.Bottom].Add(p);
                        //bitmap[p.x, p.y] = Color.Red;
                    }

                    foreach (var p in GraphicsMath.GetLinePoints((x - 1, y2_prev), (x, y2)))
                    {
                        edge.Edges[StrokeEdge.Edge.Top].Add(p);
                        //bitmap[p.x, p.y] = Color.Red;
                    }
                }

                y1_prev = y1;
                y2_prev = y2;
            }

            artBitmap.UnlockBitmap();
            shape = new Stroke(artBitmap.GetBitmap());
            //sourceStroke.Save("C:\\Users\\skura\\source\\repos\\ArtGenerator\\Output\\Shapes", "source");
            //shape.Save("C:\\Users\\skura\\source\\repos\\ArtGenerator\\Output\\Shapes", "shape2");
        }
    }
}
