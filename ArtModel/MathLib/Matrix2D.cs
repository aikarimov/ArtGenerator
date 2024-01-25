namespace ArtModel.MathLib
{
    public class Matrix2D<T>
    {
        public int Rows { get; private set; }

        public int Columns { get; private set; }

        private readonly T[,] _data;

        public T this[int row, int column]
        {
            get
            {
                return _data[row, column];
            }
            set
            {
                _data[row, column] = value;
            }
        }

        public Matrix2D(int rows, int columns)
        {
            if (rows == 0 || columns == 0)
            {
                throw new ArgumentException("Can't create an empty matrix!");
            }

            Rows = rows;
            Columns = columns;

            _data = new T[rows, columns];
        }

        public Matrix2D(int rows, int columns, T initialValue) : this(rows, columns)
        {
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    _data[i, j] = initialValue;
                }
            }
        }
    }
}
