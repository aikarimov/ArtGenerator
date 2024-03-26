using ArtGenerator.Views;
using ArtModel.Core;
using Microsoft.Win32;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ArtGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnOpenNewFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = inputPath.Text;

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                ProcessSelectedFile(filePath);
            }
        }

        private void ProcessSelectedFile(string filePath)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                Bitmap inputBitmap = (Bitmap)Image.FromStream(fileStream);

                ArtUserInput input = ArtUserInput.Default;
                input.Width = inputBitmap.Width;
                input.Height = inputBitmap.Height;

                ArtModelSerializer artModelSerializer = new ArtModelSerializer(input);

                CoreArtModel coreArtModel = new CoreArtModel(inputBitmap, artModelSerializer, new ArtModel.Core.PathSettings
                {
                    InputPath = inputPath.Text,
                    OutputPath = outputPath.Text
                });

                coreArtModel.Iterate();
            }
        }

        private void OpenNewArtPage(object sender, RoutedEventArgs e)
        {
            //NewArtPage newPage = new NewArtPage();
            //mainFrame.Content = newPage;

            OnOpenNewFile();

            /*newPage. += (sender, args) =>
            {
                button1.IsEnabled = true;
                button2.IsEnabled = true;
            };*/


            // Устанавливаем в качестве содержимого Frame

            //popupMenu.IsOpen = true;
            //OpenFileDialog();
        }

    }
}