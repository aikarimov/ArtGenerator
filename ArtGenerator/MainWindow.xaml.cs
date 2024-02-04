using ArtViewModel;
using System.Drawing;
using System.IO;
using System.Windows;

namespace ArtGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModelController viewModelController;

       private string path = @"C:\Users\skura\source\repos\ArtGenerator\ArtGenerator\Resources\kk.jpg";
       //private string path = @"C:\Users\skura\source\repos\ArtGenerator\ArtGenerator\Resources\ball.png";
        //private string path = @"C:\Users\skura\source\repos\ArtGenerator\ArtGenerator\Resources\mountains.jpg";



        public MainWindow()
        {
            InitializeComponent();

            viewModelController = new ViewModelController();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Bitmap inputBitmap = (Bitmap)Image.FromStream(fileStream);

                viewModelController.ProcessImage(inputBitmap);
            }
        }
    }
}