using ArtModel.Core.ArtificialCanvas;
using System.Drawing;

namespace ArtModel.Core
{
    public class CoreArtModel
    {
        private ArtificialCanvasGenerator _artificialCanvasGenerator;

        public CoreArtModel(Bitmap bitmap)
        {
            _artificialCanvasGenerator = new ArtificialCanvasGenerator(bitmap);

            _artificialCanvasGenerator.IterateStrokes();

            _artificialCanvasGenerator.EndIterations();
        }
    }
}
