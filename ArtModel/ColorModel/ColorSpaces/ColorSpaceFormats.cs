using ArtModel.ColorModel.ColorSpaces.Spaces;

namespace ArtModel.ColorModel.ColorSpaces
{
    public enum ColorSpaceType
    {
        RGB,
        HSV,
        CIELAB,
    }

    public static class ColorSpaceFormats
    {
        public static ColorSpaceAbstract RGBSpace { get; }
        public static ColorSpaceAbstract HSVSpace { get; }
        public static ColorSpaceAbstract CIELABSpace { get; }

        public static Dictionary<ColorSpaceType, ColorSpaceAbstract> ColorSpaces;

        static ColorSpaceFormats()
        {
            RGBSpace = new RGBSpace();
            HSVSpace = new HSVSpace();
            CIELABSpace = new CIELABSpace();

            ColorSpaces = new Dictionary<ColorSpaceType, ColorSpaceAbstract>
            {
                { ColorSpaceType.RGB, RGBSpace },
                { ColorSpaceType.HSV, HSVSpace },
                { ColorSpaceType.CIELAB, CIELABSpace },
            };
        }
    }
}
