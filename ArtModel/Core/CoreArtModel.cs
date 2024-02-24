using ArtModel.Core.ArtificialCanvas;
using ArtModel.StrokeLib;
using System.Drawing;
using System.Text.Json;

namespace ArtModel.Core
{
    public struct PathSettings
    {
        public string InputPath;
        public string OutputPath;
    }

    public class CoreArtModel
    {
        private ArtificialCanvasGenerator _artificialCanvasGenerator;

        public CoreArtModel(Bitmap bitmap, ArtModelSerializer serializer, PathSettings pathSettings)
        {
            _artificialCanvasGenerator = new ArtificialCanvasGenerator(bitmap, serializer, pathSettings);

        }

        public void Iterate()
        {
            _artificialCanvasGenerator.IterateStrokes();

            _artificialCanvasGenerator.EndIterations();
        }
    }
}
