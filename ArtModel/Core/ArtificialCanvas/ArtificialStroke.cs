using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.Core.ArtificialCanvas
{
    public class ArtificialStroke
    {
        public ArtificialStroke(int segmentCount)
        {
            SegmentCount = segmentCount;
        }

        public int SegmentCount { get; init; }
    }
}
