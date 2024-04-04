using ArtGenerator.Views;
using ArtModel.Core;
using ArtModel.ImageProccessing;
using Microsoft.Win32;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

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
                Bitmap inputBitmap = (Bitmap)System.Drawing.Image.FromStream(fileStream);

                ArtUserInput input = ArtUserInput.Default;
                // input.Width = inputBitmap.Width;
                //input.Height = inputBitmap.Height;

                ArtModelSerializer artModelSerializer = new ArtModelSerializer(input);


            }
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

            // OnOpenNewFile();

            /*newPage. += (sender, args) =>
            {
                button1.IsEnabled = true;
                button2.IsEnabled = true;
            };*/


            // Устанавливаем в качестве содержимого Frame

            //popupMenu.IsOpen = true;
            //OpenFileDialog();
        }

        private void mainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {



        }


        public void ReceiveData(ArtModelSerializer recievedSerializer, Bitmap bitmap)
        {
            var statusBar = new StatusBar();
            statusBar.Height = 40;

            // Прогресс бар
            var progressBar = new ProgressBar();
            progressBar.Width = status_stack.Width * 0.33;
            progressBar.Height = 70;
            progressBar.Minimum = 0;
            progressBar.Maximum = GetModelTotalItertations(recievedSerializer);
            progressBar.Value = 0;

            StatusBarItem statusBarItem = new StatusBarItem();
            statusBarItem.Content = progressBar;
            statusBar.Items.Add(statusBarItem);

            // Статус
            var statusTextBlock = new TextBlock();
            statusTextBlock.Text = "Подготовка...";
            statusTextBlock.TextWrapping = TextWrapping.Wrap;

            StatusBarItem textBlockItem = new StatusBarItem();
            textBlockItem.Content = statusTextBlock;
            statusBar.Items.Add(textBlockItem);

            // Добавление
            status_stack.Children.Add(statusBar);

            // Запуск

            var pathSettings = new PathSettings()
            {
                InputPath = inputPath.Text,
                OutputPath = outputPath.Text,
                LibraryPath = "C:\\Users\\skura\\source\\repos\\ArtGenerator\\ArtModel\\StrokeLib\\SourceLib"
            };

            CoreArtModel coreArtModel = new CoreArtModel(bitmap, recievedSerializer, pathSettings);

            Task.Run(() =>
            {
                var tracer = coreArtModel.CreateTracer();

                foreach (ArtBitmap art in tracer)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        progressBar.Value += 1;

                        //art.Save(pathSettings.OutputPath, $"Generation_{gen}");
                        //art.Save(pathSettings.OutputPath, $"Generation2_{gen}");

                    }));
                }
            });



        }

        private int GetModelTotalItertations(ArtModelSerializer serializer)
        {
            int total = 0;
            // Скип первого уровня, т.к. это "заполняющий" уровень
            for (int i = 1; i < serializer.Generations.Count; i++)
            {
                total += serializer.Generations[i].Iterations;
            }
            return total;
        }
    }
}