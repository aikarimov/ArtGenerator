using ArtModel.ColorModel;
using ArtModel.ColorModel.ColorSpaces;
using ArtModel.Core.ArtificialCanvas;
using ArtModel.Image.Matrix;
using System.Drawing;
using System.Drawing.Imaging;

namespace ArtModel.Core
{
    public class CoreArtModel
    {
        private ArtificialCanvasGenerator _artificialCanvasGenerator;

        public CoreArtModel(ColorSpaceType colorSpaceType, Bitmap bitmap)
        {
            MatrixBitmap inputMatrix = MatrixConverter.BitmapToMatrix(bitmap, colorSpaceType);

            _artificialCanvasGenerator = new ArtificialCanvasGenerator(inputMatrix);
        }

        public void CreateImage()
        {

        }

        public IEnumerable<MatrixBitmap> GetArtificialCanvasIterative()
        {
            yield return _artificialCanvasGenerator.GetArtificialCanvas();
        }

    }
}
