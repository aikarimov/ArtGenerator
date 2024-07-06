using ArtModel.ImageProccessing;
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

    // Главный класс, где создается Трейсер. Больше он ничего такого не делает
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

        // Созданрие трейсера, по нему будут идти итерации
        public Tracer CreateTracer(CancellationToken token)
        {
            Tracer tracer = new Tracer(_originalCanvas, _modelSerializer, _pathSettings, token);
            return tracer;
        }
    }
}
