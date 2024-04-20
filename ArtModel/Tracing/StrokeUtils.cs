using System.Drawing;
using ArtModel.ImageProccessing;

namespace ArtModel.Tracing
{
    public class MeanColorCalculator
    {
        private int R_count = 0;
        private int G_count = 0;
        private int B_count = 0;
        private int counter = 0;

        public MeanColorCalculator() { }

        public MeanColorCalculator(ArtBitmap bitmap, HashSet<(int x, int y)> pixels) : this()
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
