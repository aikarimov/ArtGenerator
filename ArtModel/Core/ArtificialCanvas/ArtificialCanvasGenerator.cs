using ArtModel.ImageModel;
using ArtModel.ImageModel.ImageProccessing;
using ArtModel.ImageModel.Tracing;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.IO.Pipes;

namespace ArtModel.Core.ArtificialCanvas
{
    public class ArtificialCanvasGenerator
    {
        private string outputPath;

        private ArtBitmap _originalCanvas;
        private ArtBitmap _shapesCanvas;
        private ArtBitmap _skeletonCanvas;
        private ArtBitmap _errorCanvas;

        public ArtificialCanvasGenerator(Bitmap bitmap, PathSettings pathSettings)
        {
            outputPath = pathSettings.OutputPath;

            _originalCanvas = new ArtBitmap(bitmap);
            _originalCanvas.Save(outputPath, "ArtOriginal");
        }

        public void IterateStrokes()
        {
            Tracer tracer = new Tracer(_originalCanvas, TracerSerializer.DefaultTracer, outputPath);

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
        }
    }
}
