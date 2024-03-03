using ArtModel.Core;
using ArtModel.Tracing;
using System.Drawing;
using System.Text.RegularExpressions;

namespace ArtModel.StrokeLib
{
    public class StrokeLibrary
    {
        private double mm_tp_px_coef = 8.6;
        static string sourceLibraryPath = "..\\..\\..\\..\\ArtModel\\StrokeLib\\SourceLib";

        private Dictionary<int, List<Stroke>> _strokesData;

        static StrokeLibrary()
        {

        }

        public StrokeLibrary(double resizeCoefficient = 1)
        {
            mm_tp_px_coef *= resizeCoefficient;
            _strokesData = StrokeLibraryReader.ReadAllStrokes(sourceLibraryPath, resizeCoefficient);
        }

        public Stroke ClassifyStroke(TracingResult targetStroke, ArtGeneration genData)
        {
            double points = targetStroke.StrokeProperties.GetProperty(StrokeProperty.Points);

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
            double width = targetStroke.StrokeProperties.GetProperty(StrokeProperty.Width);

            return _strokesData[1]
                .OrderBy(sourceStroke => Math.Abs(sourceStroke.StrokeProperties.GetProperty(StrokeProperty.Width) * mm_tp_px_coef - width))
                .First();
        }

        private Stroke ClassifyPt2(TracingResult targetStroke)
        {
            double target_width = targetStroke.StrokeProperties.GetProperty(StrokeProperty.Width);
            double target_length = targetStroke.StrokeProperties.GetProperty(StrokeProperty.Length);
            double div = target_length / target_width;
            double real_width_weight = 0;

            return _strokesData[2]
               .OrderBy(sourceStroke =>
                   {
                       double source_width = sourceStroke.StrokeProperties.GetProperty(StrokeProperty.Width);
                       double source_length = sourceStroke.StrokeProperties.GetProperty(StrokeProperty.Length);

                       return Math.Abs(source_length / source_width - div) /*+real_width_weight * Math.Abs(target_width - source_width)*/;
                   })
               .First();
        }

        private Stroke ClassifyPt3(TracingResult targetStroke)
        {
            return _strokesData[3][0];
        }

        public double CalculateResizeCoefficient(TracingResult tracingResult, Stroke strokeData)
        {
            double points = tracingResult.StrokeProperties.GetProperty(StrokeProperty.Points);
            switch (points)
            {
                case 1:
                    return (tracingResult.StrokeProperties.GetProperty(StrokeProperty.Width) / (strokeData.StrokeProperties.GetProperty(StrokeProperty.Width) * mm_tp_px_coef));
                case 2:
                    return (tracingResult.StrokeProperties.GetProperty(StrokeProperty.Width) / (strokeData.StrokeProperties.GetProperty(StrokeProperty.Width) * mm_tp_px_coef));
                   // return (tracingResult.StrokeProperties.GetProperty(StrokeProperty.Length) / strokeData.Height);
                default:
                    return (0.0);
            }
        }

    }

    public enum StartPointAlign
    {
        Center = 0,
        Bottom = 1,
    }

    file static class StrokeLibraryReader
    {
        public static Dictionary<int, List<Stroke>> ReadAllStrokes(string rootPath, double resizeCoef = 1.0)
        {
            Dictionary<int, List<Stroke>> strokes = new();

            Parallel.ForEach(Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories), filePath =>
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    Bitmap inputBitmap = (Bitmap)Image.FromStream(fileStream);
                    inputBitmap = StrokeReader.ReadStrokeCropped(inputBitmap);

                    Stroke strokeData = new Stroke(inputBitmap);
                    strokeData.Resize(resizeCoef);

                    Dictionary<string, int> newAttributes = ExtractAttributesFromPath(rootPath, filePath);
                    foreach (var kvp in newAttributes)
                    {
                        strokeData.StrokeProperties.SetProperty(kvp.Key, kvp.Value);
                    }

                    int segments = (int)strokeData.StrokeProperties.GetProperty(StrokeProperty.Points);

                    if (!strokes.ContainsKey(segments))
                    {
                        strokes.Add(segments, new List<Stroke>());
                    }

                    strokes[segments].Add(strokeData);
                }
            });

            return strokes;
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
                Dictionary<string, int> componentAttributes = ExtractAttributesFromName(component);
                foreach (var kvp in componentAttributes)
                {
                    if (!attributes.ContainsKey(kvp.Key))
                    {
                        attributes.Add(kvp.Key, kvp.Value);
                    }
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
