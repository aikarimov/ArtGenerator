namespace ArtModel.ColorModel.ColorSpaces.Spaces
{
    public class GRAYSpace : ColorSpaceBase
    {
        public override int ComponentsCount => 1;

        public override string GetColorSpaceName()
        {
            return "GrayScale";
        }

        public override PixelData ToRGB(PixelData inputPixel)
        {
            double gray = (byte)inputPixel[0];
            return new PixelData(/*[gray, gray, gray]*/);
        }

        public override PixelData FromRGB(PixelData inputPixel)
        {
            return inputPixel;
        }

        public override PixelData ToGrayscale(PixelData inputPixel)
        {
            return inputPixel;
        }
    }
}
