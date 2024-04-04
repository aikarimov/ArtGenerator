using ArtModel.Core;
using Microsoft.Win32;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Navigation;

namespace ArtGenerator.Views
{
    /// <summary>
    /// Interaction logic for NewArtPage.xaml
    /// </summary>
    public partial class NewArtPage : Page
    {
        private const string StylesFolder = "Styles";
        private const string CurrentDataFolder = "CurrentData";

        private string _executableLocation;

        private string _stylesFolderPath;
        private string _currentDataFolderPath;

        private string _jsonDataFilePath;

        private string _inputPath;

        private Bitmap? _selectedImage = null;

        private ArtUserInput? _artUserInput = null;

        private FileSystemWatcher watcher;

        private bool _loaded = false;

        private BitmapImage _bitmapImage;

        public NewArtPage(string inputPath)
        {
            InitializeComponent();
            EnsureFoldersExists();
            InitWatcher();

            this.Title = inputPath;
            _inputPath = inputPath;
        }

        private void InitWatcher()
        {
            watcher = new FileSystemWatcher();
            watcher.Path = _currentDataFolderPath;
            watcher.Filter = "*.*";

            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += ValidateJson;
            watcher.EnableRaisingEvents = true;
        }

        private void EnsureFoldersExists()
        {
            _executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            _stylesFolderPath = Path.Combine(_executableLocation, StylesFolder);
            _currentDataFolderPath = Path.Combine(_executableLocation, CurrentDataFolder);

            if (!Directory.Exists(_stylesFolderPath))
            {
                Directory.CreateDirectory(_stylesFolderPath);
            }

            if (!Directory.Exists(_currentDataFolderPath))
            {
                Directory.CreateDirectory(_currentDataFolderPath);
            }
        }

        private string ResaveJson()
        {
            ArtModelSerializer artModel = new ArtModelSerializer(ReadArtUserInput());
            string jsonData = JsonConvert.SerializeObject(artModel, Formatting.Indented);
            File.WriteAllText(_jsonDataFilePath, jsonData);
            return jsonData;
        }

        // Открыть json для редактирования
        private void button_open_json_Click(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            ResaveJson();

            Process.Start("notepad.exe", _jsonDataFilePath);
        }

        private void ValidateJson(object sender, FileSystemEventArgs e)
        {
            try
            {
                ArtModelSerializer artModel = JsonConvert.DeserializeObject<ArtModelSerializer>(File.ReadAllText(_jsonDataFilePath));

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    UserInputToTextBox(artModel.UserInput);
                    json_error_label.Visibility = Visibility.Hidden;
                }));
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    json_error_label.Visibility = Visibility.Visible;
                }));
            }
        }

        // Выбрать стиль
        private void button_pick_style_Click(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON Files (*.json)|*.json";
            openFileDialog.InitialDirectory = _stylesFolderPath;
            openFileDialog.Title = "Выбрать JSON стиль";

            if (openFileDialog.ShowDialog() == true)
            {
                string jsonFilePath = openFileDialog.FileName;

                try
                {
                    string serializedJson = File.ReadAllText(jsonFilePath);
                    ArtModelSerializer artModel = JsonConvert.DeserializeObject<ArtModelSerializer>(serializedJson)!;
                    UserInputToTextBox(artModel.UserInput);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при открытии файла: " + ex.Message);
                }
            }
        }

        // Сохранить стиль
        private void button_save_style_Click(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            // Пересохранение текущих настроек с экрана
            var jsonData = ResaveJson();

            // Сохранить сам стиль
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON Files (*.json)|*.json";
            saveFileDialog.InitialDirectory = _stylesFolderPath;
            saveFileDialog.Title = "Сохранить JSON стиль";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, jsonData);
                    MessageBox.Show("Стиль сохранён");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка сохранения стиля: " + ex.Message);
                }
            }
        }

        // Подтвердить
        private void button_confirm_Click(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;

            watcher.Dispose();

            try
            {
                string serializedJson = ResaveJson();
                ArtModelSerializer artModel = JsonConvert.DeserializeObject<ArtModelSerializer>(serializedJson)!;
                MainWindow mainWindow = (Application.Current.MainWindow as MainWindow)!;

                using (MemoryStream outStream = new MemoryStream())
                {
                    BitmapEncoder enc = new BmpBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(_bitmapImage));
                    enc.Save(outStream);
                    Bitmap bitmap = new Bitmap(outStream);

                    mainWindow.ReceiveData(artModel, bitmap);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        // Загрузка изображения
        private void button_load_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = _inputPath;

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                Title = openFileDialog.SafeFileName.Split('.')[0];

                BitmapImage logo = new BitmapImage();
                logo.BeginInit();
                logo.UriSource = new Uri(filePath);
                logo.EndInit();
                _bitmapImage = logo;
                targe_image.Source = logo;

                // Сохранение json-а в файл
                ArtModelSerializer artModel = new ArtModelSerializer(ArtUserInput.Default);
                var jsonData = JsonConvert.SerializeObject(artModel, Formatting.Indented);
                _jsonDataFilePath = Path.Combine(_currentDataFolderPath, $"{Title}.json");

                try
                {
                    File.WriteAllText(_jsonDataFilePath, jsonData);
                    _loaded = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при сохранении файла: " + ex.Message);
                }
            }
        }

        private ArtUserInput ReadArtUserInput()
        {
            return new ArtUserInput()
            {
                Generations = Convert.ToInt32(input_gen.Text),

                Segments = Convert.ToInt32(input_segments.Text),

                StrokeWidth_Min = Convert.ToInt32(input_brushW_min.Text),
                StrokeWidth_Max = Convert.ToInt32(input_brushW_max.Text),

                StrokeLength_Min = Convert.ToInt32(input_brushL_min.Text),
                StrokeLength_Max = Convert.ToInt32(input_brushL_max.Text),

                BlurSigma_Min = Convert.ToInt32(input_blur_min.Text),
                BlurSigma_Max = Convert.ToInt32(input_blur_max.Text),

                Dispersion_Min = Convert.ToInt32(input_dispersion_min.Text),
                Dispersion_Max = Convert.ToInt32(input_dispersion_max.Text)
            };
        }

        private void UserInputToTextBox(ArtUserInput input)
        {
            input_gen.Text = input.Generations.ToString();

            input_segments.Text = input.Segments.ToString();

            input_brushW_min.Text = input.StrokeWidth_Min.ToString();
            input_brushW_max.Text = input.StrokeWidth_Max.ToString();

            input_brushL_min.Text = input.StrokeLength_Min.ToString();
            input_brushL_max.Text = input.StrokeLength_Max.ToString();

            input_blur_min.Text = input.BlurSigma_Min.ToString();
            input_blur_max.Text = input.BlurSigma_Max.ToString();

            input_dispersion_min.Text = input.Dispersion_Min.ToString();
            input_dispersion_max.Text = input.Dispersion_Max.ToString();
        }

        private void button_cancel_Click(object sender, RoutedEventArgs e)
        {

            MainWindow mainWindow = (Application.Current.MainWindow as MainWindow)!;
            mainWindow.ClearFrame();

        }
    }
}
