namespace ArtModel.ColorModel
{
    public class PixelData
    {
        public static byte Components = 3;

        public PixelData()
        {
            _components = new byte[Components];
        }

        public PixelData(byte[] components)
        {
            _components = components;
        }

        public byte this[int x]
        {
            get
            {
                return _components[x];
            }
            set
            {
                _components[x] = Math.Clamp(value, (byte)0, (byte)255);
            }
        }

        private byte[] _components;

        public static PixelData operator -(PixelData a, PixelData b)
        {
            byte[] res = new byte[Components];
            for (byte i = 0; i < Components; i++)
            {
                res[i] = (byte)(a._components[i] - b._components[i]);
            }
            return new PixelData(res);
        }
    }
}
