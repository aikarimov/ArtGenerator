using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.ImageModel.Tracing
{
    public interface IStrokeLayersProvoder
    {
        HashSet<(int x, int y)> GetLayerPoints(int layer);
    }
}
