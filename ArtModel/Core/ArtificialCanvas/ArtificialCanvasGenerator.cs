using ArtModel.ColorModel.ColorSpaces;
using ArtModel.Image;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using ArtModel.ImageProccessing;
using ArtModel.MathLib;

namespace ArtModel.Core.ArtificialCanvas
{
    public class ArtificialCanvasGenerator
    {
        private string outputPath = "C:\\Users\\skura\\source\\repos\\ArtGenerator\\ArtModel\\Output";
        private string fileName = "Out";

        private ColorSpaceAbstract _colorSpace;

        private MatrixBitmap _originalCanvas;
        private MatrixBitmap _artificialCanvas;
        private MatrixBitmap _shapesCanvas;
        private MatrixBitmap _skeletonCanvas;
        private MatrixBitmap _errorCanvas;

        public ArtificialCanvasGenerator(MatrixBitmap originalMatrix)
        {
            BrightnessMap brightnessMap = new BrightnessMap();

            //_colorSpace = ColorSpaceFormats.ColorSpaces[colorSpaceType];

            MatrixBitmap blurred = GaussianBlur.ApplyGaussianBlurToRGB(originalMatrix, 4);

            MatrixConverter.WriteMatrixToFile(blurred, outputPath, fileName, ImageFormat.Png);

            /*_originalCanvas = originalMatrix;
            _artificialCanvas = new MatrixBitmap(originalMatrix);
            _shapesCanvas = new MatrixBitmap(originalMatrix);
            _skeletonCanvas = new MatrixBitmap(originalMatrix);
            _errorCanvas = new MatrixBitmap(originalMatrix);*/
        }

        private Matrix2D<double> CreateBrightnessMap(MatrixBitmap rgbImage)
        {
            int rows = rgbImage.Rows;
            int cols = rgbImage.Columns;

            Matrix2D<double> gray = new Matrix2D<double>(rows, cols);
            var rgbSpace = ColorSpaceFormats.ColorSpaces[ColorSpaceType.RGB];
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    gray[x, y] = rgbSpace.ToGrayScale(rgbImage[x, y]);
                }
            }

            return BrightnessMap.GetBrightnessMap(gray);
        }


        public MatrixBitmap GetArtificialCanvas()
        {
            return _artificialCanvas;
        }


    }
}
