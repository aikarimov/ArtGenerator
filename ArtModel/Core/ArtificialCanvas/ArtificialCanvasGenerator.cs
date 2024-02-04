using ArtModel.ImageModel;
using ArtModel.ImageModel.ImageProccessing;
using ArtModel.ImageModel.Tracing;
using ArtModel.ImageProccessing;
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

        public ArtificialCanvasGenerator(Bitmap bitmap)
        {
            _originalCanvas = new ArtBitmap(bitmap);
            _originalCanvas.Save(outputPath, "ArtOriginal");

            _artificialCanvas = new ArtBitmap(bitmap.Width, bitmap.Height);
            PaintWhite(_artificialCanvas);
        }

        public void IterateStrokes()
        {
            Tracer tracer = new Tracer(_originalCanvas, _artificialCanvas, TracerSerializer.DefaultTracer);

            tracer.GenerateArtByLayers();





        }

        private void PaintWhite(ArtBitmap artBitmap)
        {
            for (int x = 0; x < artBitmap.Width; x++)
            {
                for (int y = 0; y < artBitmap.Height; y++)
                {
                    artBitmap[x, y] = Color.White;
                }
            }
        }

        public void EndIterations()
        {
            _originalCanvas.UnlockBitmap();
            _artificialCanvas.UnlockBitmap();
        }
    }
}
