namespace Project;

public class Vector<T> : IEnumerable<T> where T : INumber<T>, IRootFunctions<T>
{
    private readonly T[] _storage;
    public int Length { get; }

    public T this[int idx]
    {
        get => _storage[idx];
        set => _storage[idx] = value;
    }

    public Vector(int length)
        => (Length, _storage) = (length, new T[length]);

    public static T operator *(Vector<T> a, Vector<T> b)
    {
        T result = T.Zero;

        for (int i = 0; i < a.Length; i++)
        {
            result += a[i] * b[i];
        }

        return result;
    }

    public static Vector<T> operator *(double constant, Vector<T> vector)
    {
        Vector<T> result = new(vector.Length);

        for (int i = 0; i < vector.Length; i++)
        {
            result[i] = vector[i] * T.CreateChecked(constant);
        }

        return result;
    }

    public static Vector<T> operator +(Vector<T> a, Vector<T> b)
    {
        Vector<T> result = new(a.Length);

        for (int i = 0; i < a.Length; i++)
        {
            result[i] = a[i] + b[i];
        }

        return result;
    }

    public static Vector<T> operator -(Vector<T> a, Vector<T> b)
    {
        Vector<T> result = new(a.Length);

        for (int i = 0; i < a.Length; i++)
        {
            result[i] = a[i] - b[i];
        }

        return result;
    }

    public static Vector<T> operator -(Vector<T> vector)
    {
        var result = new Vector<T>(vector.Length);

        for (int i = 0; i < vector.Length; i++)
        {
            result[i] = -T.One * vector[i];
        }

        return result;
    }

    public static void Copy(Vector<T> source, Vector<T> destination)
    {
        for (int i = 0; i < source.Length; i++)
        {
            destination[i] = source[i];
        }
    }

    public static Vector<T> Copy(Vector<T> otherVector)
    {
        Vector<T> newVector = new(otherVector.Length);

        Array.Copy(otherVector._storage, newVector._storage, otherVector.Length);

        return newVector;
    }

    public void Fill(T value)
    {
        for (int i = 0; i < Length; i++)
        {
            _storage[i] = T.CreateChecked(value);
        }
    }

    public T Norm() => T.Sqrt(_storage.Aggregate(T.Zero, (current, t) => current + t * t));

    public ImmutableArray<T> ToImmutableArray()
        => ImmutableArray.Create(_storage);

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_storage).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(IEnumerable<T> collection)
    {
        var enumerable = collection as T[] ?? collection.ToArray();

        if (Length != enumerable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(collection), "Sizes of vector and collection not equal");
        }

        for (int i = 0; i < Length; i++)
        {
            _storage[i] = enumerable[i];
        }
    }
}