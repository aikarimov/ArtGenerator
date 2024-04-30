using ArtGenerator.Views;
using ArtModel.Core;
using ArtModel.Statistics;
using ArtModel.Tracing;
using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Reflection;
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
        private CancellationTokenSource _tokenSource;

        public event Action NotifyCancelled;
        public event Action NotifyStart;

        private string SettingsFolder = "Settings";
        private string ImagesSubFolder = "Images";
        private string StatisticsSubFolder = "Statistics";
        private string ShapesSubFolder = "Shapes";

        public MainWindow()
        {
            InitializeComponent();
            OpenNewArtPage();
            SubscribeEvents();
            Reload();
            EnsureFoldersExists();
            LoadSettings();

            ArtStatistics.Instance = new ArtStatistics();
        }

        private void EnsureFoldersExists()
        {
            var outputFolder = outputPath.Text;
            var executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            string[] folders = [
                Path.Combine(outputFolder, ImagesSubFolder),
                Path.Combine(outputFolder, StatisticsSubFolder),
                Path.Combine(outputFolder, ShapesSubFolder),
                Path.Combine(executableLocation, SettingsFolder)];

            foreach (var f in folders)
            {
                if (!Directory.Exists(f))
                {
                    Directory.CreateDirectory(f);
                }
            }
        }

        public void ClearFrame()
        {
            NewArtPage newPage = new NewArtPage(inputPath.Text);
            mainFrame.Navigate(newPage);
            mainFrame.NavigationService.RemoveBackEntry();
        }

        private void OpenNewArtPage()
        {
            NewArtPage newPage = new NewArtPage(inputPath.Text);
            mainFrame.Navigate(newPage);

            NotifyCancelled += () =>
            {
                newPage.Reload();
                button_cancel_generation.IsEnabled = false;
            };
        }

        private void SubscribeEvents()
        {
            NotifyStart += () =>
            {
                button_cancel_generation.IsEnabled = true;
            };

            NotifyCancelled += () =>
            {
                status_stack.Children.Clear();
            };

            inputPath.TextChanged += textChangedEventHandler;
            outputPath.TextChanged += textChangedEventHandler;
            libraryPath.TextChanged += textChangedEventHandler;

            checkbox_collect_data.Checked += (s, e) => { ArtStatistics.Instance.CollectStatistics = true; };
            checkbox_collect_data.Unchecked += (s, e) => { ArtStatistics.Instance.CollectStatistics = false; };

            checkbox_shapes_map.Checked += (s, e) => { ArtStatistics.Instance.ShapesMap = true; };
            checkbox_shapes_map.Unchecked += (s, e) => { ArtStatistics.Instance.ShapesMap = false; };
        }

        private void Reload()
        {
            button_cancel_generation.IsEnabled = false;
        }

        public void ReceiveData(ArtModelSerializer recievedSerializer, Bitmap bitmap)
        {
            EnsureFoldersExists();

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

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;
            var tracer = coreArtModel.CreateTracer(token);

            // Синглтон статистики

            tracer.NotifyStatusChange += (status) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    statusTextBlock.Text = status;
                });
            };

            int generation = 0;
            tracer.NotifyGenerationsChange += (gen) => { generation = gen; };

            NotifyStart?.Invoke();

            // Запуск генерации
            Task.Run(() =>
            {
                bitmap.Save($"{pathSettings.OutputPath}\\{ImagesSubFolder}\\ArtOriginal.{ImageFormat.Png}");

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
                            artificial_render.Save($"{pathSettings.OutputPath}\\{ImagesSubFolder}", $"Render_{generation}");
                            model_render.Save($"{pathSettings.OutputPath}\\{ImagesSubFolder}", $"Model_{generation}");
                        }

                        progressBar.Value += 1;
                    });
                }

                ProccessPostArtData(tracer);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    progressBar.Value = progressBar.Maximum;
                });
            });
        }

        private void ProccessPostArtData(Tracer tracer)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string outPath = outputPath.Text;

                // Обработка статистики
                if (ArtStatistics.Instance.CollectStatistics)
                {
                    // Дисперсия регионов
                    Task.Run(() =>
                    {
                        var path = Path.Combine(outPath, StatisticsSubFolder, $"tiles-{DateTime.Now:dd-MM-HH-mm-ss}.txt");
                        foreach (var kvp in ArtStatistics.Instance.TilesData)
                        {
                            kvp.Value.Sort();
                            string json = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented);
                            File.AppendAllText(path, json);
                        }
                    });

                    // Мазки
                    Task.Run(() =>
                    {
                        var path = Path.Combine(outPath, StatisticsSubFolder, $"strokes-{DateTime.Now:dd-MM-HH-mm-ss}.txt");
                        string json = JsonConvert.SerializeObject(ArtStatistics.Instance.StrokesData, Formatting.Indented);
                        File.WriteAllText(path, json);
                    });
                }

                // Отрисовка скелетов/контуров
                Task.Run(() =>
                {
                    var shapedata = tracer.CanvasShapeGenerator.CreateShapesBitmap();
                    shapedata.shapes.Save($"{outPath}\\{ShapesSubFolder}", "Shapes");
                    shapedata.skelet.Save($"{outPath}\\{ShapesSubFolder}", "Skelet");
                });

                // Отрисовка скелетов/контуров
                Task.Run(() =>
                {
                    var errorMap = tracer.CreateErrorMap();
                    errorMap.bitmap.Save($"{outPath}\\{ShapesSubFolder}", "Error");
                    var path = Path.Combine(outPath, ShapesSubFolder, $"errorData.txt");
                    File.WriteAllText(path, errorMap.dispersion.ToString());
                });
            }); 
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
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

        // Досрочно завершить генерацию. В отличие от простого закрытия программы выполнит отрисовку карты скелетов, итд
        private void button_cancel_generation_click(object sender, RoutedEventArgs e)
        {
            _tokenSource.Cancel();
            NotifyCancelled?.Invoke();
        }

        // Сейв система настроек
        private void LoadSettings()
        {
            var executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var path = Path.Combine(executableLocation, SettingsFolder, $"settings.json");
            try
            {
                var settings = JsonConvert.DeserializeObject<PathSettings>(File.ReadAllText(path));
                inputPath.Text = settings.InputPath;
                outputPath.Text = settings.OutputPath;
                libraryPath.Text = settings.LibraryPath;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Ошибка при сохранении файла: " + ex.Message);
            }
        }

        private void textChangedEventHandler(object sender, TextChangedEventArgs args)
        {
            try
            {
                var settings = new PathSettings()
                {
                    InputPath = inputPath.Text,
                    OutputPath = outputPath.Text,
                    LibraryPath = libraryPath.Text
                };

                var settingsJsonData = JsonConvert.SerializeObject(settings, Formatting.Indented);
                var executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                var path = Path.Combine(executableLocation, SettingsFolder, $"settings.json");
                File.WriteAllText(path, settingsJsonData);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении файла: " + ex.Message);
            }
        }

        private void checkbox_shapes_map_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
