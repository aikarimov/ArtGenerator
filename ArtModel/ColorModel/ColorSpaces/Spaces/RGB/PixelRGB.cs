using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.ColorModel.ColorSpaces.Spaces.RGB
{
    public class PixelRGB : Pixel<byte>
    {
        public override int ComponentsCount { get => 3; }
    }
}
