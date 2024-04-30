using ArtModel.Core;
using ArtModel.Tracing;
using System.Drawing;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ArtModel.StrokeLib
{
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
            return (value - MinValue) / Interval;
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

    public class StrokeLibrary
    {
        public static double mm_tp_px_coef = 8.6;
        private string sourceLibraryPath;

        private StrokePropertyNormalizator _normaliztor;
        private Dictionary<int, List<Stroke>> _strokesData;

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

            _normaliztor = new StrokePropertyNormalizator();
            InitNormalizator(_normaliztor, serializer);

            StrokeLibraryReader.ReadAllStrokes(sourceLibraryPath, _strokesData, resizeCoefficient);
        }

        private void InitNormalizator(StrokePropertyNormalizator normaliztor, ArtModelSerializer serializer)
        {
            var input = serializer.UserInput;

            normaliztor.AddP(StrokeProperty.Width, 1 * 8.6);
            normaliztor.AddP(StrokeProperty.Width, 6 * 8.6);

            normaliztor.AddP(StrokeProperty.Angle1, 90);
            normaliztor.AddP(StrokeProperty.Angle1, 180);

            normaliztor.AddP(StrokeProperty.Fraction, 0); // Первый сегмент почти 0
            normaliztor.AddP(StrokeProperty.Fraction, 100); // Первый сегмент почти весь мазок

            normaliztor.AddP(StrokeProperty.LtoW, input.StrokeLength_Max / input.StrokeWidth_Min);
            normaliztor.AddP(StrokeProperty.LtoW, input.StrokeLength_Min / input.StrokeWidth_Max);
        }

        public Stroke ClassifyStroke(TracingResult targetStroke, ArtGeneration genData)
        {
            double points = targetStroke.SP.GetP(StrokeProperty.Points);

            switch (points)
            {
                case 1:
                    return ClassifyPt1(targetStroke).Copy();
                case 2:
                    return ClassifyPt2(targetStroke).Copy();
                case 3:
                    return ClassifyPt3(targetStroke).Copy();
                default:
                    goto case 1;
            }
        }

        private Stroke ClassifyPt1(TracingResult targetStroke)
        {
            double width = targetStroke.SP.GetP(StrokeProperty.Width);

            return _strokesData[1]
                .MinBy(sourceStroke => Math.Abs(sourceStroke.SP.GetP(StrokeProperty.Width) - width))!
                .Copy();
        }

        private Stroke ClassifyPt2(TracingResult targetStroke)
        {
            return _strokesData[2]
               .MinBy(sourceStroke =>
                   {
                       return Math.Abs(targetStroke.SP.GetP(StrokeProperty.LtoW) - sourceStroke.SP.GetP(StrokeProperty.LtoW));
                   })!
               .Copy();
        }

        private Stroke ClassifyPt3(TracingResult targetStroke)
        {
            double target_ltow = _normaliztor.Normalize(StrokeProperty.LtoW, targetStroke.SP.GetP(StrokeProperty.LtoW));
            double target_angle = _normaliztor.Normalize(StrokeProperty.Angle1, Math.Abs(targetStroke.SP.GetP(StrokeProperty.Angle1)));
            double target_fraction = _normaliztor.Normalize(StrokeProperty.Fraction, targetStroke.SP.GetP(StrokeProperty.Fraction));
            double target_width = _normaliztor.Normalize(StrokeProperty.Width, targetStroke.SP.GetP(StrokeProperty.Width));

            Stroke result = _strokesData[3]
               .MinBy(sourceStroke =>
               {
                   double source_ltow = _normaliztor.Normalize(StrokeProperty.LtoW, sourceStroke.SP.GetP(StrokeProperty.LtoW));
                   double source_angle = _normaliztor.Normalize(StrokeProperty.Angle1, sourceStroke.SP.GetP(StrokeProperty.Angle1));
                   double source_fraction = _normaliztor.Normalize(StrokeProperty.Fraction, sourceStroke.SP.GetP(StrokeProperty.Fraction));
                   double source_width = _normaliztor.Normalize(StrokeProperty.Width, sourceStroke.SP.GetP(StrokeProperty.Width));

                   double df_ltow = Math.Abs(target_ltow - source_ltow);
                   double df_angle = Math.Abs(target_angle - source_angle);
                   double df_fraction = Math.Abs(target_fraction - source_fraction);
                   double df_width = Math.Abs(target_width - source_width);

                   return Math.Sqrt(
                       Math.Pow(df_ltow, 2) +
                       Math.Pow(df_angle, 2) +
                       Math.Pow(df_fraction, 2) +
                       0.33 * Math.Pow(df_width, 2));
               })!
               .Copy();

            double a1 = targetStroke.SP.GetP(StrokeProperty.Angle1);
            double a2 = result.SP.GetP(StrokeProperty.Angle1);

            try
            {
                if (/*a1 != double.NaN && a2 != double.NaN && */(Math.Sign(a1) != Math.Sign(a2)))
                {
                    result.Flip(RotateFlipType.RotateNoneFlipX);
                    result.NormalMap.Flip(RotateFlipType.RotateNoneFlipX);
                }
            }
            catch { }

            return result;
        }

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

    public static class StrokeLibraryReader
    {
        public static void ReadAllStrokes(string rootPath, Dictionary<int, List<Stroke>> data, double resizeCoef = 1.0)
        {
            Parallel.ForEach(Directory.GetFiles(rootPath, "*.png", SearchOption.AllDirectories), filePath =>
            {
                // игонрируем карты нормалей и контуры
                if (filePath.EndsWith("n.png")) return;

                string fileName = filePath;
                string fileNameNormal = fileName.Insert(filePath.IndexOf('.'), "n");

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

                // Нормаль мазка
                using (FileStream fileStream = new FileStream(fileNameNormal, FileMode.Open, FileAccess.Read))
                {
                    Stroke normal = new Stroke(StrokeReader.ReadStrokeCropped((Bitmap)Image.FromStream(fileStream), false));
                    normal.Resize(resizeCoef);

                    mask.NormalMap = normal;
                }

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