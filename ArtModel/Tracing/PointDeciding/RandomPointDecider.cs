using ArtModel.ImageProccessing;

namespace ArtModel.Tracing.PointDeciding
{
    public class RandomPointDecider : IPointDecider
    {
        private RandomPoolGenerator _pool;

        private const double FirstLayerFill = 0.01;

        public RandomPointDecider() { }

        public RandomPointDecider(ArtBitmap original, int randomSeed = -1)
        {
            _pool = new RandomPoolGenerator(original.Width, original.Height, randomSeed);
        }

        public (int x, int y) GetNewPoint()
        {
            return _pool.GetFromPoolRandom();
        }

        public bool DeciderAvaliable()
        {
            return (_pool.PoolAvaliable() && _pool.PoolPercent() > FirstLayerFill);
        }

        public void PointCallback((int x, int y) point)
        {
            _pool.RemoveFromPool(point);
        }

        public void PostStroke()
        {

        }
    }
}
