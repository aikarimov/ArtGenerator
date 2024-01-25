using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.ColorModel.ColorSpaces.Spaces
{
    public class CIELABSpace : ColorSpaceBase
    {
        public override int ComponentsCount => 3;

        public override PixelData FromRGB(PixelData inputPixel)
        {
            throw new NotImplementedException();
        }

        public override string GetColorSpaceName()
        {
            throw new NotImplementedException();
        }

        public override PixelData ToGrayscale(PixelData inputPixel)
        {
            throw new NotImplementedException();
        }

        public override PixelData ToRGB(PixelData inputPixel)
        {
            throw new NotImplementedException();
        }
    }
}
