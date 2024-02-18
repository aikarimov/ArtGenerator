using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.ImageModel.Tracing.Circle
{
    public interface ITracingResult
    {
        public StrokePropertyCollection StrokeProperties { get; set; }

        public Color MeanColor { get; set; }
    }

    public class TracingResult : ITracingResult
    {
        public TracingResult()
        {
            StrokeProperties = new StrokePropertyCollection();
        }

        public StrokePropertyCollection StrokeProperties { get; set; }

        public Color MeanColor { get; set; }

        public HashSet<(int x, int y)> Coordinates { get; set; }

        public double MainAngle { get; set; }
    }
}
