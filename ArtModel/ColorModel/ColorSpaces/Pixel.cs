namespace ArtModel.ColorModel.ColorSpaces
{
    public abstract class Pixel<T>
    {
        public abstract int ComponentsCount { get; }

        private T[] _components;

        public Pixel()
        {
            _components = new T[ComponentsCount];
        }

        public Pixel(T[] components)
        {
            _components = components;
        }

        public virtual T this[int index]
        {
            get => _components[index];
            set => _components[index] = value;
        }
    }
}