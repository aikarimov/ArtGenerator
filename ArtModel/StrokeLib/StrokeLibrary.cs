using ArtModel.Core;
using ArtModel.Tracing;
using System.Drawing;
using System.Text.RegularExpressions;

namespace ArtModel.StrokeLib
{
    // Классы для нормализации значений, ведь евклидово расстьояние будем искать не по абсолютным, но нормализованным велчиинам
    public class NormalizationParam
    {
        public NormalizationParam(double initialValue)
        {
            MinValue = initialValue;
            MaxValue = initialValue;
        }

        public double MinValue { get; set; }

        public double MaxValue { get; set; }

        public double Interval => MaxValue - MinValue + 1;

        public void AddValue(double value)
        {
            MinValue = value < MinValue ? value : MinValue;
            MaxValue = value > MaxValue ? value : MaxValue;
        }

        public double Normalize(double value)
        {
            return Math.Clamp((value - MinValue) / Interval, 0, 1);
        }
    }

    public class StrokePropertyNormalizator : StrokePropertyCollection<NormalizationParam>
    {
        public void AddP(StrokeProperty key, double value)
        {
            if (!CheckPropery(key))
            {
                SetP(key, new NormalizationParam(value));
            }
            else
            {
                GetP(key).AddValue(value);
            }
        }

        public double Normalize(StrokeProperty key, double value)
        {
            return GetP(key).Normalize(value);
        }
    }

    public enum ResizeCoefficient
    {
        Width,
        Length,
        Middle,
    }

    // Класс для рабоыт с библиотекой мазков.
    public class StrokeLibrary
    {
        // Коэффициент пересчёта из условных единиц в библиотеке (l15) в пиксели (15 * mm_tp_px_coef)
        public static double mm_tp_px_coef = 8.6;
        private string sourceLibraryPath;

        private Dictionary<int, List<Stroke>> _strokesData;
        private Dictionary<int, StrokePropertyNormalizator> _normalizators;

        private object locker = new object();

        public StrokeLibrary(string libFolder, ArtModelSerializer serializer, double resizeCoefficient = 1)
        {
            sourceLibraryPath = libFolder;
            mm_tp_px_coef *= resizeCoefficient;

            _strokesData = new()
                {
                    { 1, new List<Stroke>() },
                    { 2, new List<Stroke>() },
                    { 3, new List<Stroke>() }
                };

            _normalizators = new()
                {
                    { 1, new StrokePropertyNormalizator() },
                    { 2, new StrokePropertyNormalizator() },
                    { 3, new StrokePropertyNormalizator() }
                };

            InitNormalizator(serializer);
            StrokeLibraryReader.ReadAllStrokes(sourceLibraryPath, _strokesData, resizeCoefficient);
        }

        private void InitNormalizator(ArtModelSerializer serializer)
        {
            var input = serializer.UserInput;

            // Мазки с разными сегментами имеют разные параметры нормализации и разные границы в библиотеке. Поэтому руками вбиваем,
            // Какие паарметры нормализации мы ожидаем из программы

            // 2 точки
            _normalizators[2].AddP(StrokeProperty.Width, 1 * mm_tp_px_coef);
            _normalizators[2].AddP(StrokeProperty.Width, 9 * mm_tp_px_coef);

            _normalizators[2].AddP(StrokeProperty.LtoW, input.StrokeLength_Max / input.StrokeWidth_Min);
            _normalizators[2].AddP(StrokeProperty.LtoW, input.StrokeLength_Min / input.StrokeWidth_Max);

            // 3 точки
            _normalizators[3].AddP(StrokeProperty.Width, 1 * mm_tp_px_coef);
            _normalizators[3].AddP(StrokeProperty.Width, 6 * mm_tp_px_coef);

            _normalizators[3].AddP(StrokeProperty.Angle1, 90);
            _normalizators[3].AddP(StrokeProperty.Angle1, 180);

            _normalizators[3].AddP(StrokeProperty.Fraction, 0); // Первый сегмент почти 0
            _normalizators[3].AddP(StrokeProperty.Fraction, 100); // Первый сегмент почти весь мазок

            _normalizators[3].AddP(StrokeProperty.LtoW, input.StrokeLength_Max / input.StrokeWidth_Min);
            _normalizators[3].AddP(StrokeProperty.LtoW, input.StrokeLength_Min / input.StrokeWidth_Max);
        }

        // Классификатор (1, 2, 3 точечные мазки). Вся классификация - это выбор мазка с минимальным евклидовым расстоянием.
        public Stroke ClassifyStroke(TracingResult targetStroke, ArtGeneration genData)
        {
            lock (locker)
            {
                double points = targetStroke.SP.GetP(StrokeProperty.Points);

                Stroke result;

                switch (points)
                {
                    case 1:
                        result = ClassifyPt1(targetStroke);
                        break;
                    case 2:
                        result = ClassifyPt2(targetStroke);
                        break;
                    case 3:
                        result = ClassifyPt3(targetStroke);
                        break;
                    default:
                        goto case 1;
                }

                return result;
            }

        }

        // 1-точечный классифицируем только по реальной ширине, у него нет незхависимых характеристик
        private Stroke ClassifyPt1(TracingResult targetStroke)
        {
            double width = targetStroke.SP.GetP(StrokeProperty.Width);

            return _strokesData[1]
                .MinBy(sourceStroke => Math.Abs(sourceStroke.SP.GetP(StrokeProperty.Width) - width))!.Copy();
        }

        // 2-точечный классифицируем по отношению длины к ширине, а также вводим дополнительный весовой коэффициент учитывающий реальную ширину
        // Это нужно, так как даже если отношение длины/ширине идеально - разница реального размера может быть слишком велика. И тогда
        // Если мазкок сильнро увеличить - будет было. Уменьшить - пиксели. Поэтому мы можем чуть-чут ьпожертвовать идеальным отношением,
        // Но зато получить мазок, который не надо будет очень сильно рескейлить.
        private Stroke ClassifyPt2(TracingResult targetStroke)
        {
            double target_ltow = _normalizators[2].Normalize(StrokeProperty.LtoW, targetStroke.SP.GetP(StrokeProperty.LtoW));
            double target_width = _normalizators[2].Normalize(StrokeProperty.Width, targetStroke.SP.GetP(StrokeProperty.Width));

            return _strokesData[2]
               .MinBy(sourceStroke =>
                   {
                       double source_ltow = _normalizators[2].Normalize(StrokeProperty.LtoW, sourceStroke.SP.GetP(StrokeProperty.LtoW));
                       double source_width = _normalizators[3].Normalize(StrokeProperty.Width, sourceStroke.SP.GetP(StrokeProperty.Width));

                       double df_ltow = Math.Abs(target_ltow - source_ltow);
                       double df_width = Math.Abs(target_width - source_width);

                       return Math.Sqrt(Math.Pow(df_ltow, 2) + 0.33 * Math.Pow(df_width, 2));
                   })!.Copy();
        }

        // Логика с версовым коээфициентом аналогична PT2. Однако тут ещё классфиицруем по углу и по fraction. Fraction - доля 1го сегмента от всей длины.
        // Ну и по отношению длины к ширине.
        private Stroke ClassifyPt3(TracingResult targetStroke)
        {
            double target_ltow = _normalizators[3].Normalize(StrokeProperty.LtoW, targetStroke.SP.GetP(StrokeProperty.LtoW));
            double target_angle = _normalizators[3].Normalize(StrokeProperty.Angle1, Math.Abs(targetStroke.SP.GetP(StrokeProperty.Angle1)));
            double target_fraction = _normalizators[3].Normalize(StrokeProperty.Fraction, targetStroke.SP.GetP(StrokeProperty.Fraction));
            double target_width = _normalizators[3].Normalize(StrokeProperty.Width, targetStroke.SP.GetP(StrokeProperty.Width));

            Stroke result = _strokesData[3]
               .MinBy(sourceStroke =>
               {
                   double source_ltow = _normalizators[3].Normalize(StrokeProperty.LtoW, sourceStroke.SP.GetP(StrokeProperty.LtoW));
                   double source_angle = _normalizators[3].Normalize(StrokeProperty.Angle1, sourceStroke.SP.GetP(StrokeProperty.Angle1));
                   double source_fraction = _normalizators[3].Normalize(StrokeProperty.Fraction, sourceStroke.SP.GetP(StrokeProperty.Fraction));
                   double source_width = _normalizators[3].Normalize(StrokeProperty.Width, sourceStroke.SP.GetP(StrokeProperty.Width));

                   double df_ltow = Math.Abs(target_ltow - source_ltow);
                   double df_angle = Math.Abs(target_angle - source_angle);
                   double df_fraction = Math.Abs(target_fraction - source_fraction);
                   double df_width = Math.Abs(target_width - source_width);

                   return Math.Sqrt(
                       Math.Pow(df_ltow, 2) +
                       Math.Pow(df_angle, 2) +
                       Math.Pow(df_fraction, 2) +
                       0.33 * Math.Pow(df_width, 2)); // Весовой коэффициенит для учёта реальных размеров
               })!.Copy();

            // Отзеркаливание мазка, если углы не совпадают
            try
            {
                double a1 = targetStroke.SP.GetP(StrokeProperty.Angle1);
                double a2 = result.SP.GetP(StrokeProperty.Angle1);

                if (Math.Sign(a1) != Math.Sign(a2))
                {
                    result.Flip(RotateFlipType.RotateNoneFlipX);
                    result.NormalMap?.Flip(RotateFlipType.RotateNoneFlipX);
                }
            }
            catch { }

            return result;
        }

        // Расчёт коээфициента рескейлап мазка. По дефолту идёт выравнивание по ширине.
        public double CalculateResizeCoefficient(TracingResult tracingResult, Stroke strokeData, ResizeCoefficient resizeCoefficient = ResizeCoefficient.Width)
        {
            switch (resizeCoefficient)
            {
                case ResizeCoefficient.Width:
                    return (tracingResult.SP.GetP(StrokeProperty.Width) / (strokeData.SP.GetP(StrokeProperty.Width)));
                case ResizeCoefficient.Length:
                    return (tracingResult.SP.GetP(StrokeProperty.Length) / (strokeData.SP.GetP(StrokeProperty.Length)));
                case ResizeCoefficient.Middle:
                    return (
                        CalculateResizeCoefficient(tracingResult, strokeData, ResizeCoefficient.Width) +
                        CalculateResizeCoefficient(tracingResult, strokeData, ResizeCoefficient.Length)) / 2;
                default:
                    return 1;
            }
        }
    }

    // Считывание мазков из файла
    public static class StrokeLibraryReader
    {
        public static void ReadAllStrokes(string rootPath, Dictionary<int, List<Stroke>> data, double resizeCoef = 1.0)
        {
            Parallel.ForEach(Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories), filePath =>
            {
                // игонрируем карты нормалей
                if (filePath.EndsWith("n.png") || filePath.EndsWith("n.jpg")) return;

                string fileName = filePath;
                string fileNameNoFormat = fileName.Remove(filePath.Length - 4); // Убираем формат
                string fileNameNormal = $"{fileNameNoFormat}n.png";

                Stroke mask;
                int points;

                // Маска мазка
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    mask = new Stroke(StrokeReader.ReadStrokeCropped((Bitmap)Image.FromStream(fileStream), true));
                    mask.Resize(resizeCoef);

                    foreach (var kvp in ExtractAttributesFromPath(rootPath, filePath))
                    {
                        StrokeProperty property = StrokePropertyCollection<double>.StrokePropertyByAlias(kvp.Key);
                        mask.SP.SetP(property, kvp.Value);
                    }
                    RecaclulateIndirectProperties(mask.SP);

                    points = (int)mask.SP.GetP(StrokeProperty.Points);
                }

                // Нормаль мазка. Если она не найдена, то будет null в свойстве stroke
                try
                {
                    using (FileStream fileStream = new FileStream(fileNameNormal, FileMode.Open, FileAccess.Read))
                    {
                        Stroke normal = new Stroke(StrokeReader.ReadStrokeCropped((Bitmap)Image.FromStream(fileStream), false));
                        normal.Resize(resizeCoef);

                        mask.NormalMap = normal;
                    }
                }
                catch { }

                data[points].Add(mask);
            });
        }

        // Посчитать непрямые свойства, которые идут на основе других
        private static void RecaclulateIndirectProperties(StrokePropertyCollection<double> collection)
        {
            try
            {
                double width = collection.GetP(StrokeProperty.Width) * StrokeLibrary.mm_tp_px_coef;
                collection.SetP(StrokeProperty.Width, width);

                double length = collection.GetP(StrokeProperty.Length) * StrokeLibrary.mm_tp_px_coef;
                collection.SetP(StrokeProperty.Length, length);

                collection.SetP(StrokeProperty.LtoW, length / width);

                double angle = collection.GetP(StrokeProperty.Angle1);
                collection.SetP(StrokeProperty.Angle1, 180 - angle);
            }
            catch { }
        }

        // Получние аттрибутов из пути /w1l5/s1a1 -> {w, 1}, {l, 5}, {s, 1}, {a, 1}
        private static Dictionary<string, int> ExtractAttributesFromPath(string rootPath, string filePath)
        {
            Dictionary<string, int> attributes = new();

            string relativePath = Path.GetRelativePath(rootPath, filePath);
            relativePath = relativePath.Substring(0, relativePath.IndexOf('.')); // Убрать разрешение

            string[] components = relativePath.Split(Path.DirectorySeparatorChar);

            foreach (var component in components)
            {
                foreach (var kvp in ExtractAttributesFromName(component))
                {
                    attributes.TryAdd(kvp.Key, kvp.Value);
                }
            }

            return attributes;
        }

        // Получение аттрибутов из строки. w1l5 -> {w, 1}, {l, 5}
        private static Dictionary<string, int> ExtractAttributesFromName(string name)
        {
            Dictionary<string, int> attributes = new();
            Regex regex = new Regex(@"([a-zA-Z]+)(\d+)");
            MatchCollection matches = regex.Matches(name);

            foreach (Match match in matches)
            {
                string key = match.Groups[1].Value;
                string value = match.Groups[2].Value;
                attributes[key] = Convert.ToInt32(value);
            }

            return attributes;
        }
    }
}