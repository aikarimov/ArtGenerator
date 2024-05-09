using ArtModel.Core;
using ArtModel.ImageProccessing;
using ArtModel.MathLib;
using System.Collections.Generic;
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

        private HashSet<(int x, int y)> Contour;

        public StrokeShape(Stroke stroke)
        {
            sourceStroke = stroke;
            Width = stroke.Width;
            Height = stroke.Height;
            edge = new StrokeEdge();
            Contour = new();
        }

        public Stroke GetShape()
        {
            return shape;
        }

        public void Rotate(double rotationAngle, Color fillColor)
        {
            double relAngle = rotationAngle - Math.PI / 2;
            double cosA = Math.Abs(Math.Cos(relAngle));
            double sinA = Math.Abs(Math.Sin(relAngle));
            int newWidth = (int)(cosA * Width + sinA * Height);
            int newHeight = (int)(cosA * Height + sinA * Width);

            double angle = rotationAngle + 1.5 * Math.PI;
            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);

            HashSet<(int x, int y)> rotatedEdges = new();
            foreach (var edge in edge.Edges.Values)
            {
                for (int i = 0; i < edge.Count - 1; i++)
                {
                    var p1 = RotatePoint(edge.ElementAt(i));
                    var p2 = RotatePoint(edge.ElementAt(i + 1));

                    if (Math.Abs(p1.x - p2.x) > 1 || Math.Abs(p1.y - p2.y) > 1)
                    {
                        foreach (var p in GraphicsMath.GetLinePoints(p1, p2))
                        {
                            rotatedEdges.Add(p);
                        }
                    }
                    rotatedEdges.Add(p1);
                    rotatedEdges.Add(p2);
                }
            }

            shape.UnlockBitmap();
            var sourceBitmap = shape.GetBitmap();

            var rotatedBitmap = new ArtBitmap(newWidth, newHeight);
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

            shape = new Stroke(rotatedBitmap.GetBitmap());

            Width = newWidth;
            Height = newHeight;

            foreach (var p in rotatedEdges)
            {
                shape[p.x, p.y] = Color.Red;
            }

            (int x, int y) RotatePoint((int x, int y) p)
            {
                p = (p.x - Width / 2, p.y - Height / 2);
                int xnew = (int)(p.x * cos - p.y * sin);
                int ynew = (int)(p.x * sin + p.y * cos);
                return (xnew + newWidth / 2, ynew + newHeight / 2);
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
                        edge.Edges[StrokeEdge.Edge.Left].Add(p);;
                    }
                }

                // Правый
                else
                if (x == Width - 1)
                {
                    foreach (var p in GraphicsMath.GetLinePoints((x, y1), (x, y2)))
                    {
                        edge.Edges[StrokeEdge.Edge.Right].Add(p);
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
                    }

                    foreach (var p in GraphicsMath.GetLinePoints((x - 1, y2_prev), (x, y2)))
                    {
                        edge.Edges[StrokeEdge.Edge.Top].Add(p);
                    }
                }

                y1_prev = y1;
                y2_prev = y2;
            }

            artBitmap.UnlockBitmap();
            shape = new Stroke(artBitmap.GetBitmap());
        }
    }
}
