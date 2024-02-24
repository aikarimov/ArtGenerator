using ArtGenerator.Views;
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
        private HomeViewModel _homeViewModel;

        //private bool

        public MainWindow()
        {
            InitializeComponent();

            _homeViewModel = new HomeViewModel();
            DataContext = _homeViewModel;
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

                _homeViewModel.NewImageProcess(inputBitmap, new ArtModel.Core.PathSettings
                {
                    InputPath = inputPath.Text,
                    OutputPath = outputPath.Text
                });
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