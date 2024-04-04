using ArtGenerator.Views;
using ArtModel.Core;
using Microsoft.Win32;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

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

        public void ClearFrame()
        {
            //mainFrame.Content = null;
            mainFrame.NavigationService.RemoveBackEntry();


            mainFrame.NavigationService.Refresh();
        }

        private void OpenNewArtPage(object sender, RoutedEventArgs e)
        {
            NewArtPage newPage = new NewArtPage(inputPath.Text);
            mainFrame.Navigate(newPage);
        }

        private void mainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {



        }

        public void ReceiveData(ArtModelSerializer recievedSerializer, Bitmap bitmap)
        {
            var statusBar = new StatusBar();
            statusBar.Height = 40;

            // Прогресс бар
            var progressBar = new ProgressBar()
            {
                Width = 500,
                Height = 100,
                Minimum = 0,
                Maximum = GetModelTotalItertations(recievedSerializer),
                Value = 0
            };

            StatusBarItem statusBarItem = new StatusBarItem()
            {
                Width = 500,
                Content = progressBar
            };

            statusBar.Items.Add(statusBarItem);

            // Статус
            var statusTextBlock = new TextBlock()
            {
                Text = "[]",
                TextWrapping = TextWrapping.Wrap
            };

            StatusBarItem textBlockItem = new StatusBarItem()
            {
                Content = statusTextBlock
            };

            statusBar.Items.Add(textBlockItem);

            // Добавление
            status_stack.Children.Add(statusBar);

            // Настройка
            var pathSettings = new PathSettings()
            {
                InputPath = inputPath.Text,
                OutputPath = outputPath.Text,
                LibraryPath = libraryPath.Text
            };

            CoreArtModel coreArtModel = new CoreArtModel(bitmap, recievedSerializer, pathSettings);

            CancellationToken token = new CancellationToken();
            var tracer = coreArtModel.CreateTracer(token);
            tracer.NotifyStatusChange += (status) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    statusTextBlock.Text = status;
                });
            };

            int generation = 0;
            tracer.NotifyGenerationsChange += (gen) => { generation = gen; };

            // Запуск генерации
            Task.Run(() =>
            {


                /* if ((bool)checkbox_save_to_folder.IsChecked!)
                 {
                     bitmap.Save(pathSettings.OutputPath, "ArtOriginal");
                 }*/



                foreach (var art in tracer)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var artificial_render = art.Item1;
                        var model_render = art.Item2;

                        // Отобраение прогресса на экране
                        if ((bool)checkbox_show_progress.IsChecked!)
                        {
                            bitmap_image.Source = BitmapToImageSource(artificial_render.GetBitmap());
                        }

                        // Сохранение текущего слоя в папку
                        if ((bool)checkbox_save_to_folder.IsChecked!)
                        {
                            artificial_render.Save(pathSettings.OutputPath, $"Render_{generation}");
                            model_render.Save(pathSettings.OutputPath, $"Model_{generation}");
                        }

                        progressBar.Value += 1;
                    });
                }
            });
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private int GetModelTotalItertations(ArtModelSerializer serializer)
        {
            int total = 0;
            for (int i = 0; i < serializer.Generations.Count; i++)
            {
                total += serializer.Generations[i].Iterations;
            }
            return total;
        }
    }
}
