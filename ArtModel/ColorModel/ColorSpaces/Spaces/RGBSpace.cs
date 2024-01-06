namespace ArtModel.ColorModel.ColorSpaces.Spaces
{
    public class RGBSpace : ColorSpaceAbstract
    {
        public override int ComponentsCount { get; init; } = 3;

        public override double ToGrayScale(in PixelData pixel)
        {
            double R = pixel.Get(0);
            double G = pixel.Get(1);
            double B = pixel.Get(2);
            return (byte)Math.Clamp(0.2126 * R + 0.7152 * G + 0.0722 * B, 0, 255);
        }
    }
}
