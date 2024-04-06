using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.Tracing
{
    public class TracingResult
    {
        public TracingResult()
        {
            SP = new StrokePropertyCollection<double>();
        }

        public StrokePropertyCollection<double> SP { get; set; }

        public Color MeanColor { get; set; }

        public double Dispersion { get; set; }

        public HashSet<(int x, int y)> Coordinates { get; set; }

        public double MainAbsAngle { get; set; }
    }
}
