using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using ArtModel.Core;

namespace ArtViewModel
{
    public class HomeViewModel
    {
        public HomeViewModel()
        {

        }

        public void ProcessImage(Bitmap bitmap, PathSettings pathSettings)
        {
            CoreArtModel model = new CoreArtModel(bitmap, pathSettings);

            model.Iterate();
        }
    }
}
