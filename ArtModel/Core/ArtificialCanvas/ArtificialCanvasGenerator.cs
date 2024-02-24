using ArtModel.ImageProccessing;
using ArtModel.Tracing;
using System.Drawing;

namespace ArtModel.Core.ArtificialCanvas
{
    public class ArtificialCanvasGenerator
    {
        private string outputPath;

        private ArtBitmap _originalCanvas;
        //private ArtBitmap _shapesCanvas;
        //private ArtBitmap _skeletonCanvas;
        //private ArtBitmap _errorCanvas;
        private ArtModelSerializer _modelSerializer;

        public ArtificialCanvasGenerator(Bitmap bitmap, ArtModelSerializer serializer, PathSettings pathSettings)
        {
            _modelSerializer = serializer;

            outputPath = pathSettings.OutputPath;

            _originalCanvas = new ArtBitmap(bitmap);
        }

        public void IterateStrokes()
        {
            Tracer tracer = new Tracer(_originalCanvas, _modelSerializer, outputPath);

            tracer.GenerateArtByLayers();
        }

        public void EndIterations()
        {
            _originalCanvas.UnlockBitmap();
        }
    }
}
