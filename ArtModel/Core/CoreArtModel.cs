using ArtModel.ImageProccessing;
using ArtModel.StrokeLib;
using ArtModel.Tracing;
using System.Drawing;
using System.Text.Json;

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

            _pathSettings.LibraryPath = "..\\..\\..\\..\\ArtModel\\StrokeLib\\SourceLib";
        }

        public void Iterate()
        {
            Tracer tracer = new Tracer(_originalCanvas, _modelSerializer, _pathSettings);

            foreach (var bitmap in tracer)
            {

            }



            _originalCanvas.UnlockBitmap();
        }
    }
}
