using System.Drawing;

namespace ArtModel.ImageModel.Tracing
{
    public class StrokeUtils
    {
        private static Color GetMeanColor(in List<Color> pixels)
        {
            int a_R = 0;
            int a_G = 0;
            int a_B = 0;

            foreach (var pixel in pixels)
            {
                a_R += pixel.R;
                a_G += pixel.G;
                a_B += pixel.B;
            }

            a_R /= pixels.Count;
            a_G /= pixels.Count;
            a_B /= pixels.Count;

            return Color.FromArgb(a_R, a_G, a_B);
        }

        public static double GetDispersion(ArtBitmap bitmap, in HashSet<(int x, int y)> pixels, in Color meanColor)
        {
            double sum = 0.0;
            foreach (var pixel in pixels)
            {
                double eucl = CalculateSquaredEuclideanDistance(bitmap[pixel.x, pixel.y], meanColor);
                sum += eucl;
            }

            return (sum / (pixels.Count()));

            static double CalculateSquaredEuclideanDistance(in Color c1, in Color c2)
            {
                double R_sq = Math.Pow(c1.R - c2.R, 2);
                double G_sq = Math.Pow(c1.G - c2.G, 2);
                double B_sq = Math.Pow(c1.B - c2.B, 2);
                return R_sq + G_sq + B_sq;
            }
        }
    }

    public class MeanColorCalculator
    {
        private int R_count = 0;
        private int G_count = 0;
        private int B_count = 0;
        private int counter = 0;

        public MeanColorCalculator() { }

        public MeanColorCalculator(in ArtBitmap bitmap, in HashSet<(int x, int y)> pixels) : this()
        {
            foreach (var pixel in pixels)
            {
                R_count += bitmap[pixel.x, pixel.y].R;
                G_count += bitmap[pixel.x, pixel.y].G;
                B_count += bitmap[pixel.x, pixel.y].B;
                counter++;
            }
        }

        public MeanColorCalculator Copy()
        {
            return new MeanColorCalculator()
            {
                R_count = this.R_count,
                G_count = this.G_count,
                B_count = this.B_count,
                counter = this.counter
            };
        }

        public void MergeWith(MeanColorCalculator calc)
        {
            this.R_count += calc.R_count;
            this.G_count += calc.G_count;
            this.B_count += calc.B_count;
            this.counter += calc.counter;
        }

        public void AddColor(in Color color)
        {
            R_count += color.R;
            G_count += color.G;
            B_count += color.B;
            counter++;
        }

        public void Reset()
        {
            R_count = 0;
            G_count = 0;
            B_count = 0;
            counter = 0;
        }

        public Color GetMeanColor()
        {
            return Color.FromArgb(byte.MaxValue,
                Math.Clamp(R_count / counter, 0, 255),
                Math.Clamp(G_count / counter, 0, 255),
                Math.Clamp(B_count / counter, 0, 255));
        }
    }
}
