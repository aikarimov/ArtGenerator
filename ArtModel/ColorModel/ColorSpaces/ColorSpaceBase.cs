namespace ArtModel.ColorModel.ColorSpaces
{
    public abstract class ColorSpaceBase
    {
        public abstract int ComponentsCount { get; }

        public abstract string GetColorSpaceName();

        public abstract PixelData ToRGB(PixelData inputPixel);

        public abstract PixelData FromRGB(PixelData inputPixel);

        public abstract PixelData ToGrayscale(PixelData inputPixel);
    }
}
