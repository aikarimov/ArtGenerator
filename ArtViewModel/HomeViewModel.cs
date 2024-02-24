using ArtModel.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Input;


namespace ArtViewModel
{
    public class HomeViewModel : BaseViewModel
    {
        public HomeViewModel()
        {
            InitializeCommands();
        }

        public ICommand OpenFileCommand { get; private set; }

        public ICommand ProcessDataCommand { get; private set; }
        public PathSettings PathSettings { get; private set; }

        private void InitializeCommands()
        {
            // ProcessDataCommand = new RelayCommand(ProcessData);
        }


        public void NewImageProcess(Bitmap bitmap, PathSettings pathSettings)
        {
            ArtUserInput input = ArtUserInput.Default;
            input.Width = bitmap.Width;
            input.Height = bitmap.Height;

            ArtModelSerializer artModelSerializer = new ArtModelSerializer(input);

            CoreArtModel coreArtModel = new CoreArtModel(bitmap, artModelSerializer, pathSettings);

            coreArtModel.Iterate();
        }
    }
}
