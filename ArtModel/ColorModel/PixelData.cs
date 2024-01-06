namespace ArtModel.ColorModel
{
    public class PixelData
    {
        public PixelData(int componentsCount)
        {
            _components = new double[componentsCount];
            for (int i = 0; i < componentsCount; i++)
            {
                _components[i] = 0;
            }
        }

        public PixelData(double[] components)
        {
            _components = components;
        }

        private double[] _components;

        public double Get(int i) => _components[i];

        public void Set(int i, double data) => _components[i] = data;
    }
}
