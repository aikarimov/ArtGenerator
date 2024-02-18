using ArtModel.Core.ArtificialCanvas;
using ArtModel.StrokeLib;
using System.Drawing;

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

        public CoreArtModel(Bitmap bitmap, PathSettings pathSettings)
        {
            _artificialCanvasGenerator = new ArtificialCanvasGenerator(bitmap, pathSettings);   
        }

        public void Iterate()
        {
            
            //LibReader.ProcessFiles("C:\\Users\\skura\\source\\repos\\ArtGenerator\\ArtModel\\StrokeLib\\SourceLib");



            _artificialCanvasGenerator.IterateStrokes();

            _artificialCanvasGenerator.EndIterations();
        }
    }
}
