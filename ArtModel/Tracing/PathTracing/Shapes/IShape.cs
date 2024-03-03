using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.Tracing.PathTracing.Shapes
{
    public interface IShape
    {
        public bool IsInside((int x, int y) point);
    }
}
