using ArtModel.ImageModel;
using ArtModel.ImageModel.Tracing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArtModel.StrokeLib
{
    public enum StartPointAlign
    {
        Center = 0,
        Bottom = 1,
    }

    public class StrokeLibraryReader
    {
        public static Dictionary<int, List<Stroke>> ReadAllStrokes(string rootPath, double resizeCoef = 1.0)
        {
            Dictionary<int, List<Stroke>> strokes = new();

            foreach (var filePath in Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories))
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
            }

            return strokes;
        }

        // Получение точки приложения кисти (центр битмапы для 1 сегмента, центр нижней грани для 2+ сегментов)
        public static (int x, int y) FindPivotPoint(ArtBitmap artBitmap, StartPointAlign align)
        {
            if (align == StartPointAlign.Center)
            {
                return (artBitmap.Width / 2, artBitmap.Height / 2);
            }
            else
            {
                int width = artBitmap.Width;
                int x1 = 0;
                int x2 = width;

                for (int i = 0; i < width; i++)
                {
                    if (artBitmap[i, 0].R <= 240)
                    {
                        x1 = i;
                        break;
                    }
                }

                for (int i = width - 1; i > 0; i--)
                {
                    if (artBitmap[i, 0].R <= 240)
                    {
                        x2 = i;
                        break;
                    }
                }

                return ((x1 + x2) / 2, 0);
            }
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
