using ArtModel.ImageProccessing;
using ArtModel.StrokeLib;
using ArtModel.Tracing;
using System.Drawing;

namespace ArtModel.Core
{
    public struct PathSettings
    {
        public string InputPath;
        public string OutputPath;
        public string LibraryPath;
    }

    public class CoreArtModel
    {
        private ArtBitmap _originalCanvas;

        private ArtModelSerializer _modelSerializer;

        private PathSettings _pathSettings;

        public CoreArtModel(Bitmap bitmap, ArtModelSerializer serializer, PathSettings pathSettings)
        {
            _originalCanvas = new ArtBitmap(bitmap);
            _modelSerializer = serializer;
            _pathSettings = pathSettings;
        }

        public Tracer CreateTracer()
        {
            CancellationToken token = new CancellationToken();
            Tracer tracer = new Tracer(_originalCanvas, _modelSerializer, _pathSettings, token);
            return tracer;
        }
    }
}
