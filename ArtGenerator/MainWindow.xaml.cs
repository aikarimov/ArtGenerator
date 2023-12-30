using ArtModel.ImageProccessing;
using System.Runtime.Intrinsics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var source = LoadBitmap(path);
            Image1.Source = source;

            var orig = ImageFiltering.BitmapToMatrix(source);

            var blurred = GaussianBlur.ApplyGaussianBlur(orig, 1);
            var blurredImg = ImageFiltering.MatrixToBitmap(blurred);
            Image2.Source = blurredImg;

            var grayMatrix = ImageFiltering.ToGrayScale(blurred);
            var grayImg = ImageFiltering.MatrixToBitmap(grayMatrix);
            Image3.Source = grayImg;

            var sobel = ImageFiltering.ApplySobel(grayMatrix);
            var sobelImg = ImageFiltering.MatrixToBitmap(sobel);
            Image4.Source = sobelImg;
        }

        private string path = @"C:\Users\skura\source\repos\ArtGenerator\ArtGenerator\Resources\Valve.png";
        //private string path = @"C:\Users\skura\source\repos\ArtGenerator\ArtGenerator\Resources\Anime2.jpg";

        private BitmapSource LoadBitmap(string path)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                bitmap.EndInit();

                return bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}");
                return null;
            }
        }
    }
}