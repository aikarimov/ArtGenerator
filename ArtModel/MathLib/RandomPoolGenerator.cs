namespace ArtModel.MathLib
{
    public class RandomPoolGenerator
    {
        private HashSet<(int, int)> _coordsData;

        private Random _random;

        public RandomPoolGenerator(int width, int height, int randomSeed)
        {
            _coordsData = new HashSet<(int, int)>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _coordsData.Add((x, y));
                }
            }

            _random = new Random(randomSeed);
        }

        public void RemoveFromPool(HashSet<(int, int)> pixels)
        {
            foreach (var pixel in pixels)
            {
                _coordsData.Remove(pixel);
            }
        }

        public bool GetFromPool(out (int x, int y) coords)
        {
            int rand = _random.Next(_coordsData.Count);
            coords = _coordsData.ElementAt(rand);
            _coordsData.Remove(coords);

            return (_coordsData.Count > 0);
        }
    }
}
