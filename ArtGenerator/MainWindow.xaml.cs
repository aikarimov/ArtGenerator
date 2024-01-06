using ArtModel.ImageProccessing;
using ArtViewModel;
using System.Drawing;
using System.Windows;

namespace ArtGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModelController viewModelController;

        private string path = @"C:\Users\skura\source\repos\ArtGenerator\ArtGenerator\Resources\Valve.png";

        public MainWindow()
        {
            InitializeComponent();

            viewModelController = new ViewModelController();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            viewModelController.ProcessImage(path);
        }
    }
}