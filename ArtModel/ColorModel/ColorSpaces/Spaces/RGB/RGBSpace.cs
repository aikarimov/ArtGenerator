namespace ArtModel.ColorModel.ColorSpaces.Spaces
{
    public class RGBSpace : ColorSpaceBase
    {
        public override int ComponentsCount => 3;

        public override string GetColorSpaceName()
        {
            return "RGB";
        }

        public override PixelData ToRGB(PixelData inputPixel)
        {
            return inputPixel;
        }

        public override PixelData FromRGB(PixelData inputPixel)
        {
            return inputPixel;
        }

        public override PixelData ToGrayscale(PixelData inputPixel)
        {
            double R = inputPixel[0];
            double G = inputPixel[1];
            double B = inputPixel[2];
            return new PixelData(/*[Math.Clamp(0.2126 * R + 0.7152 * G + 0.0722 * B, 0, 255)]*/);
        }
    }
}
