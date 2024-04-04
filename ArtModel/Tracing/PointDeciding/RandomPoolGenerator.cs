namespace ArtModel.Tracing.PointDeciding
{
    public class RandomPoolGenerator
    {
        private HashSet<(int, int)> _coordsData;
        private double _initialSize;
        private Random _random;
        private int _width;
        private int _height;

        public RandomPoolGenerator(int width, int height, int randomSeed = -1)
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
            _width = width;
            _height = height;

            _random = randomSeed == -1 ? new Random() : new Random(randomSeed);
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

        public (int x, int y) GetRandomPoint()
        {
            int randX = _random.Next(_width);
            int randY = _random.Next(_height);
            return (randX, randY);
        }


        public (int x, int y) GetFromPoolRandom()
        {
            int rand = _random.Next(_coordsData.Count);
            (int x, int y) pixel = _coordsData.ElementAt(rand);
            _coordsData.Remove(pixel);
            return pixel;
        }

        public IEnumerable<(int x, int y)> GetFromPool()
        {
            foreach (var coords in _coordsData)
            {
                yield return coords;
            }
        }
    }
}
