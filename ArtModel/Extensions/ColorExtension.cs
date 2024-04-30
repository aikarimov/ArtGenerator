using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.Extensions
{
    public static class ColorExtension
    {
        public static double GetAverage(this Color color)
        {
            return (byte)Math.Clamp((color.R + color.G + color.B) / 3, 0, 255);
        }
    }
}
