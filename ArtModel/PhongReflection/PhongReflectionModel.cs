using ArtModel.MathLib;
using ArtModel.StrokeLib;
using System.Drawing;

namespace ArtModel.PhongReflection
{
    public class PhongReflectionParameters
    {
        public double AmbientStrenght;
        public double DiffuseStrenght;
        public double SpecularStrenght;
        public int Shininess;
        public Vector3 LightDirection;
        public Vector3 ObserverDirection;
        public Color AmbientColor;
        public Color SpecularColor;

        public PhongReflectionParameters(Color ambientColor)
        {
            AmbientStrenght = 0.5f;
            DiffuseStrenght = 0.5f;
            SpecularStrenght = 0.5f;
            Shininess = 40;
            LightDirection = new Vector3(0.16f, 0.16f, -0.97f);
            ObserverDirection = new Vector3(0f, 0f, -1f);
            AmbientColor = ambientColor;
            SpecularColor = Color.FromArgb(255, 255, 255);
        }
    }

    public class PhongReflectionModel
    {
        // Средний цвет, после которого стоит применять модель Фонга
        public static double BorderColorBrightness = (200 + 200 + 200) / 3;

        public static Stroke ApplyReflection(Stroke stroke, Stroke normalsMap, PhongReflectionParameters parameters)
        {
            Stroke reflectionStroke = new Stroke(new Bitmap(stroke.Width, stroke.Height));
            reflectionStroke.SP = stroke.SP;
            reflectionStroke.PivotPoint = stroke.PivotPoint;

            Color strokeColor = parameters.AmbientColor;
            Color specularColor = parameters.SpecularColor;
            var light = parameters.LightDirection;

            for (int x = 0; x < stroke.Width; x++)
            {
                for (int y = 0; y < stroke.Height; y++)
                {
                    Color normalPixel = normalsMap[x, y];

                    if (stroke[x, y].R < Stroke.BLACK_BORDER_STRONG)
                    {
                        double r = normalPixel.R / 255f * 2 - 1;
                        double g = normalPixel.G / 255f * 2 - 1;
                        double b = (normalPixel.B - 128) / 128f * -1;
                        Vector3 normal = new Vector3(r, g, b);

                        // Эмбиент составляющая
                        double ambient = parameters.AmbientStrenght;

                        // Диффузная составляющая
                        double diffusion = Math.Clamp(Vector3.Dot(normal, light), 0, 255);

                        // Бликовая составляющая
                        Vector3 reflection = normal * 2 * diffusion - light;
                        double specular = (float)Math.Pow(Vector3.Dot(reflection, parameters.ObserverDirection), parameters.Shininess);

                        double red = ambient * strokeColor.R + parameters.DiffuseStrenght * diffusion * strokeColor.R + parameters.SpecularStrenght * specular * specularColor.R;
                        double green = ambient * strokeColor.G + parameters.DiffuseStrenght * diffusion * strokeColor.G + parameters.SpecularStrenght * specular * specularColor.G;
                        double blue = ambient * strokeColor.B + parameters.DiffuseStrenght * diffusion * strokeColor.B + parameters.SpecularStrenght * specular * specularColor.B;

                        red = Math.Clamp(red, 0, 255);
                        green = Math.Clamp(green, 0, 255);
                        blue = Math.Clamp(blue, 0, 255);

                        reflectionStroke[x, y] = Color.FromArgb((byte)red, (byte)green, (byte)blue);
                    }
                    else
                    {
                        reflectionStroke[x, y] = Color.White;
                    }
                }
            }

            return reflectionStroke;
        }
    }
}
