using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArtModel.Core;
using static ArtModel.ColorModel.ColorSpaces.ColorSpaceType;

namespace ArtViewModel
{
    public class ViewModelController
    {
        public ViewModelController()
        {

        }

        public void ProcessImage(string inputPath)
        {
            Bitmap bitmap = new Bitmap(inputPath);
            CoreArtModel model = new CoreArtModel(RGB, bitmap);
            //model.CreateImage();
        }
    }
}
