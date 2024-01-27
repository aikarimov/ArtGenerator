using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
            CoreArtModel model = new CoreArtModel(bitmap);
        }
    }
}
