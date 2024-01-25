using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArtModel.ColorModel.ColorSpaces;
using ArtModel.Core;

namespace ArtViewModel
{
    public class ViewModelController
    {
        public ViewModelController()
        {

        }

        public void ProcessImage(Bitmap bitmap)
        {
            CoreArtModel model = new CoreArtModel(ColorSpaceType.RGB, bitmap);
        }
    }
}
