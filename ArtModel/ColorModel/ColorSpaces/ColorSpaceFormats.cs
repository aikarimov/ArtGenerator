using ArtModel.ColorModel.ColorSpaces.Spaces;

namespace ArtModel.ColorModel.ColorSpaces
{
    public enum ColorSpaceType
    {
        RGB,
        HSV,
        CIELAB,
        GRAYSCALE
    }

    public static class ColorSpaceFormats
    {
        private static ColorSpaceBase RGBSpace { get; set; }
        private static ColorSpaceBase HSVSpace { get; set; }
        private static ColorSpaceBase CIELABSpace { get; set; }
        private static ColorSpaceBase GRAYSCALESpace { get; set; }

        public static Dictionary<ColorSpaceType, ColorSpaceBase> ColorSpaces;

        static ColorSpaceFormats()
        {
            RGBSpace = new RGBSpace();
            HSVSpace = new HSVSpace();
            CIELABSpace = new CIELABSpace();
            GRAYSCALESpace = new GRAYSpace();

            ColorSpaces = new Dictionary<ColorSpaceType, ColorSpaceBase>
            {
                { ColorSpaceType.RGB, RGBSpace },
                { ColorSpaceType.HSV, HSVSpace },
                { ColorSpaceType.CIELAB, CIELABSpace },
                { ColorSpaceType.GRAYSCALE, GRAYSCALESpace },
            };
        }
    }
}
