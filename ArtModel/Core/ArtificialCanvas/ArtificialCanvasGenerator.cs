using ArtModel.ImageModel;
using ArtModel.ImageModel.ImageProccessing;
using ArtModel.ImageModel.Tracing;
using System.Drawing;
using System.IO;

namespace ArtModel.Core.ArtificialCanvas
{
    public class ArtificialCanvasGenerator
    {
        private string outputPath = "C:\\Users\\skura\\source\\repos\\ArtGenerator\\ArtModel\\Output";

        private ArtBitmap _originalCanvas;
        private ArtBitmap _artificialCanvas;
        private ArtBitmap _shapesCanvas;
        private ArtBitmap _skeletonCanvas;
        private ArtBitmap _errorCanvas;

        private Random _random = new Random(/*Guid.NewGuid().GetHashCode()*/ 1);

        private Dictionary<(int, int), bool> _allCoordinates;

        public ArtificialCanvasGenerator(Bitmap bitmap)
        {
            _originalCanvas = new ArtBitmap(bitmap);
            _originalCanvas.Save(outputPath, "ArtOriginal");

            _artificialCanvas = new ArtBitmap(bitmap.Width, bitmap.Height);

            _allCoordinates = new Dictionary<(int, int), bool>();
            InitCoordinates();

            void InitCoordinates()
            {
                for (int x = 0; x < _originalCanvas.Width; x++)
                {
                    for (int y = 0; y < _originalCanvas.Height; y++)
                    {
                        _allCoordinates.Add((x, y), true);
                    }
                }
            }
        }

        public void IterateStrokes()
        {
            double[,] brightnessMap = BrightnessMap.GetBrightnessMap(_originalCanvas);

            int counter = 0;

            while (GetFromPixelPool(out var coordinates))
            {
                int x = coordinates.x;
                int y = coordinates.y;

                TracingResult path = Tracer.GetIterativeTracePath(_originalCanvas, (x, y), brightnessMap[y, x]);
                GetFromPixelPool(path.Coordinates);

                WritePixels(path.Coordinates, path.MeanColor);

                if (counter % 2500 == 0)
                {
                    _artificialCanvas.Save(outputPath, "Artificial" + counter);
                }

                counter++;
            }

            _artificialCanvas.Save(outputPath, "Artificial");
        }

        public void GetFromPixelPool(List<(int, int)> pixels)
        {
            foreach (var pixel in pixels)
            {
                _allCoordinates.Remove(pixel);
            }
        }

        private bool GetFromPixelPool(out (int x, int y) coords)
        {
            int rand = _random.Next(_allCoordinates.Count);
            coords = _allCoordinates.ElementAt(rand).Key;
            _allCoordinates.Remove(coords);

            return (_allCoordinates.Count > 0);
        }

        private void WritePixels(List<(int x, int y)> coordonates, Color color)
        {
            foreach (var c in coordonates)
            {
                _artificialCanvas[c.x, c.y] = color;
            }
        }

        public void EndIterations()
        {
            _originalCanvas.UnlockBitmap();
            _artificialCanvas.UnlockBitmap();
        }
    }
}
