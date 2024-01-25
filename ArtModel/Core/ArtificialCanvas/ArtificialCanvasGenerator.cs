using System.Drawing.Imaging;
using ArtModel.Image.Matrix;
using ArtModel.MathLib;
using ArtModel.ImageProccessing;
using ArtModel.ColorModel;
using ArtModel.Image.StrokeModel;
using ArtModel.ColorModel.ColorSpaces;

namespace ArtModel.Core.ArtificialCanvas
{
    public class ArtificialCanvasGenerator
    {
        private string outputPath = "C:\\Users\\skura\\source\\repos\\ArtGenerator\\ArtModel\\Output";

        private ColorSpaceBase _colorSpace;

        private MatrixBitmap _originalCanvas;
        private MatrixBitmap _artificialCanvas;
        private MatrixBitmap _shapesCanvas;
        private MatrixBitmap _skeletonCanvas;
        private MatrixBitmap _errorCanvas;

        private int _iterationsCount = 1;

        private Random _random;

        public ArtificialCanvasGenerator(MatrixBitmap originalMatrix)
        {
            _originalCanvas = originalMatrix;
            _artificialCanvas = new MatrixBitmap(originalMatrix);
            _random = new Random(Guid.NewGuid().GetHashCode());

            /*MatrixBitmap blurred = GaussianBlur.ApplyGaussianBlurToRGB(originalMatrix, 1);
            MatrixBitmap grayMap = blurred.ToGrayscale();
            MatrixBitmap brightnessMap = BrightnessMap.GetBrightnessMap(grayMap);*/

            IterateStrokes();

            MatrixConverter.WriteMatrixToFile(_artificialCanvas, outputPath, "art", ImageFormat.Png);
        }

        private void IterateStrokes()
        {
            //MatrixBitmap grayMap = _originalCanvas.ToGrayscale();
            //MatrixBitmap brightnessMap = BrightnessMap.GetBrightnessMap(grayMap);

            double tol = 4;

            for (int step = 0; step < 10000; step++)
            {
                new Thread (() => 
                {
                    (int x, int y) coords = GetRandomCoords();
                    int x = coords.x;
                    int y = coords.y;

                    // double angleNorm = (brightnessMap[x, y][0] + Math.PI / 2) % (2 * Math.PI);

                    /*int x2 = x + (int)(10 * Math.Cos(angleNorm));
                    int y2 = y + (int)(10 * Math.Sin(angleNorm));*/

                    int radius = 10;
                    while (radius > 0)
                    {
                        CircleMask circle = StrokeCircleMask.ApplyCircleMask(_originalCanvas, x, y, radius);
                        var disp = StrokeUtils.GetDispersion(circle.Data);

                        if (disp.dispersion < tol || radius == 1)
                        {
                            WritePixelsWithColor(circle.Coordinates, disp.average);
                            break;
                        }
                        else
                        {
                            radius -= 1;
                        }
                    }
                }).Start();
                
            }
        }

        private (int x, int y) GetRandomCoords()
        {
            int rx = _random.Next(0, _originalCanvas.Width);
            int ry = _random.Next(0, _originalCanvas.Height);
            return (rx, ry);
        }

        private void WritePixelsWithColor(List<(int x, int y)> coordonates, PixelData color)
        {
            foreach (var c in coordonates)
            {
                _artificialCanvas[c.x, c.y] = color;
            }
        }

        /*private (PixelData color, int radius) CalculateFirstROI(int x, int y)
        {
            double tol = 
        }
*/













        public MatrixBitmap GetArtificialCanvas()
        {
            return _artificialCanvas;
        }
    }
}
