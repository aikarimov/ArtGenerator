using System.Drawing;

namespace ArtModel.StrokeLib
{
    public class StrokeResizer
    {
        public static Bitmap ResizeImage(Bitmap originalImage, int newWidth, int newHeight)
        {
            Bitmap resizedImage = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(resizedImage))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                // Рисуем уменьшенное изображение
                g.DrawImage(originalImage, new Rectangle(0, 0, newWidth, newHeight));
            }

            return resizedImage;
        }
    }
}
