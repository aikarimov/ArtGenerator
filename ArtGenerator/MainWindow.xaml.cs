using ArtViewModel;
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
        private HomeViewModel viewModelController;
 
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenFileDialog()
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
            viewModelController = new HomeViewModel();

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                Bitmap inputBitmap = (Bitmap)Image.FromStream(fileStream);

                viewModelController.ProcessImage(inputBitmap, new ArtModel.Core.PathSettings
                {
                    InputPath = inputPath.Text,
                    OutputPath = outputPath.Text
                });
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //popupMenu.IsOpen = true;
           OpenFileDialog();
        }

    }
}