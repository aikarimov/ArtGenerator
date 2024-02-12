using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.ImageModel.Tracing
{
    public class LayerStroke<TProvider> where TProvider : IStrokeLayersProvoder
    {
        private Dictionary<int, HashSet<(int x, int y)>> _maskLayers = new();

        public LayerStroke()
        {

        }

        public void Setlayer(int layer)
        {
            if (layer > _maskLayers.Count)
            {
                for (int i = _maskLayers.Count; i <= layer; i++)
                {

                }
            }
            else
            {

            }
        }

        public IEnumerable<(int x, int y)> GetAllValues()
        {
            for (int i = _maskLayers.Count - 1; i >= 0; i--)
            {
                for (int j = _maskLayers[i].Count - 1; j >= 0; j--)
                {
                    yield return _maskLayers[i].ElementAt(j);
                }
            }
        }

        public bool Contains((int x, int y) point)
        {
            foreach (var value in GetAllValues())
            {
                if (value == point)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
