namespace ArtModel.MathLib
{
    public class RandomPoolGenerator
    {
        private HashSet<(int, int)> _coordsData;

        private double _initialSize;

        private Random _random;

        public RandomPoolGenerator(int width, int height)
        {
            _coordsData = new HashSet<(int, int)>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _coordsData.Add((x, y));
                }
            }
            _initialSize = _coordsData.Count;
            _random = new Random(0);
        }

        public void RemoveFromPool(HashSet<(int, int)> pixels)
        {
            foreach (var pixel in pixels)
            {
                _coordsData.Remove(pixel);
            }
        }

        public void RemoveFromPool((int, int) pixel)
        {
            _coordsData.Remove(pixel);
        }

        public bool PoolAvaliable()
        {
            return (_coordsData.Count > 0);
        }

        public double PoolPercent()
        {
            return (_coordsData.Count / _initialSize);
        }

        public (int x, int y) GetFromPool()
        {
            int rand = _random.Next(_coordsData.Count);
            (int x, int y) coords = _coordsData.ElementAt(rand);
            _coordsData.Remove(coords);
            return coords;
        }
    }
}
