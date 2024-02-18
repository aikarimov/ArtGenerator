using ArtModel.ImageModel;
using ArtModel.ImageModel.Tracing;
using ArtModel.ImageModel.Tracing.Circle;
using ArtModel.StrokeLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static ArtModel.ImageModel.Tracing.GenerationData;

namespace ArtModel.StrokeLib
{
    public class Stroke : ArtBitmap
    {
        public Stroke(Bitmap bitmap) : base(bitmap)
        {
            StrokeProperties = new StrokePropertyCollection();
            PivotPoint = (Width / 2, Height / 2);
        }

        public (int x, int y) PivotPoint { get; private set; }

        public StrokePropertyCollection StrokeProperties { get; private set; }

        public Stroke Copy()
        {
            return new Stroke((Bitmap)_bitmap.Clone())
            {
                StrokeProperties = this.StrokeProperties,
                PivotPoint = this.PivotPoint,
            };
        }

        public void Resize(double coefficient)
        {
            UnlockBitmap();

            Width = (int)Math.Ceiling((Width * coefficient));
            Height = (int)Math.Ceiling(Height * coefficient);

            Bitmap resizedImage = new Bitmap(Width, Height);
            using (Graphics g = Graphics.FromImage(resizedImage))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(_bitmap, new Rectangle(0, 0, Width, Height));
            }

            _bitmap.Dispose();
            _bitmap = resizedImage;

            LockBitmap();
        }

        public void Rotate(double rotationAngle, out (int x, int y) pivot)
        {
            UnlockBitmap();

            double relAngle = rotationAngle - Math.PI / 2;

            //rotationAngle -= (Math.PI / 2);
            //while (rotationAngle < 0) rotationAngle += Math.PI * 2;

            double cosA = Math.Abs(Math.Cos(relAngle));
            double sinA = Math.Abs(Math.Sin(relAngle));
            int newWidth = (int)(cosA * Width + sinA * Height);
            int newHeight = (int)(cosA * Height + sinA * Width);
            var rotatedBitmap = new Bitmap(newWidth, newHeight);
            rotatedBitmap.SetResolution(_bitmap.HorizontalResolution, _bitmap.VerticalResolution);

            using (Graphics g = Graphics.FromImage(rotatedBitmap))
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 255, 255)))
                {
                    g.FillRectangle(brush, 0, 0, newWidth, newHeight);
                }

                g.TranslateTransform(rotatedBitmap.Width / 2, rotatedBitmap.Height / 2);
                g.RotateTransform((float)(-relAngle * 180 / Math.PI));
                g.TranslateTransform(-_bitmap.Width / 2, -_bitmap.Height / 2);
                g.DrawImage(_bitmap, new Point(0, 0));
            }

            _bitmap.Dispose();
            _bitmap = rotatedBitmap;

            pivot = CalculatePivot();

            Width = newWidth;
            Height = newHeight;

            LockBitmap();

            (int x, int y) CalculatePivot()
            {
                double segments = StrokeProperties.GetProperty(StrokeProperty.Points);
                int cenX = newWidth / 2;
                int cenY = newHeight / 2;

                if (segments == 1)
                {
                    return (cenX, cenY);
                }
                else /*if (segments == 2) */
                {
                    int x = cenX + (int)(0.5 * Height * Math.Cos((rotationAngle + Math.PI)));
                    int y = cenY + (int)(0.5 * Height * Math.Sin((rotationAngle + Math.PI)));
                    return (x, y);
                }
            }
        }
    }

    public class StrokeLibrary
    {
        private double mm_tp_px_coef = 8.6;
        static string sourceLibraryPath = "C:\\Users\\skura\\source\\repos\\ArtGenerator\\ArtModel\\StrokeLib\\SourceLib";
        //static string localLibraryPath = "C:\\Users\\skura\\source\\repos\\ArtGenerator\\ArtModel\\StrokeLib\\LocalLib";

        private Dictionary<int, List<Stroke>> _strokesData;

        static StrokeLibrary()
        {

        }

        public StrokeLibrary(float resizeCoefficient)
        {
            _strokesData = StrokeLibraryReader.ReadAllStrokes(sourceLibraryPath, 1);
        }

        public Stroke ClassifyStroke(TracingResult targetStroke, SingleGenerationData genData)
        {
            double points = targetStroke.StrokeProperties.GetProperty(StrokeProperty.Points);

            switch (points)
            {
                case 1:
                    return ClassifyPt1(targetStroke).Copy();
                case 2:
                    return ClassifyPt2(targetStroke, genData).Copy();
                case 3:
                    return ClassifyPt3(targetStroke).Copy();
                default:
                    goto case 1;
            }
        }

        private Stroke ClassifyPt1(TracingResult targetStroke)
        {
            double width = targetStroke.StrokeProperties.GetProperty(StrokeProperty.Width);

            return _strokesData[1]
                .OrderBy(sourceStroke => Math.Abs(sourceStroke.StrokeProperties.GetProperty(StrokeProperty.Width) * mm_tp_px_coef - width))
                .First();
        }

        private Stroke ClassifyPt2(TracingResult targetStroke, SingleGenerationData genData)
        {
            double target_width = targetStroke.StrokeProperties.GetProperty(StrokeProperty.Width);
            double target_length = targetStroke.StrokeProperties.GetProperty(StrokeProperty.Length);
            double div = target_length / target_width;
            double real_width_weight = 0;

            return _strokesData[2]
               .OrderBy(sourceStroke =>
                   {
                       double source_width = sourceStroke.StrokeProperties.GetProperty(StrokeProperty.Width);
                       double source_length = sourceStroke.StrokeProperties.GetProperty(StrokeProperty.Length);

                       return Math.Abs(source_length / source_width - div) /*+real_width_weight * Math.Abs(target_width - source_width)*/;
                   })
               .First();
        }

        private Stroke ClassifyPt3(TracingResult targetStroke)
        {
            return _strokesData[3][0];
        }

        public double CalculateResizeCoefficient(TracingResult tracingResult, Stroke strokeData)
        {
            double points = tracingResult.StrokeProperties.GetProperty(StrokeProperty.Points);
            switch (points)
            {
                case 1:
                    return (tracingResult.StrokeProperties.GetProperty(StrokeProperty.Width) / (strokeData.StrokeProperties.GetProperty(StrokeProperty.Width) * mm_tp_px_coef));
                case 2:
                    return (tracingResult.StrokeProperties.GetProperty(StrokeProperty.Width) / (strokeData.StrokeProperties.GetProperty(StrokeProperty.Width) * mm_tp_px_coef));
                default:
                    return (0.0);
            }
        }

    }
}
