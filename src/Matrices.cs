namespace Project;

public class SparseMatrix
{
    public int[] Ig { get; init; }
    public int[] Jg { get; init; }
    public double[] Di { get; }
    public double[] Gg { get; }
    public int Size { get; }

    public SparseMatrix(int size, int sizeOffDiag)
    {
        Size = size;
        Ig = new int[size + 1];
        Jg = new int[sizeOffDiag];
        Gg = new double[sizeOffDiag];
        Di = new double[size];
    }

    public static Vector<double> operator *(SparseMatrix matrix, Vector<double> vector)
    {
        Vector<double> product = new(vector.Length);

        for (int i = 0; i < vector.Length; i++)
        {
            product[i] = matrix.Di[i] * vector[i];

            for (int j = matrix.Ig[i]; j < matrix.Ig[i + 1]; j++)
            {
                product[i] += matrix.Gg[j] * vector[matrix.Jg[j]];
                product[matrix.Jg[j]] += matrix.Gg[j] * vector[i];
            }
        }

        return product;
    }

    public void PrintDense(string path)
    {
        double[,] A = new double[Size, Size];

        for (int i = 0; i < Size; i++)
        {
            A[i, i] = Di[i];

            for (int j = Ig[i]; j < Ig[i + 1]; j++)
            {
                A[i, Jg[j]] = Gg[j];
                A[Jg[j], i] = Gg[j];
            }
        }

        using var sw = new StreamWriter(path);
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                sw.Write(A[i, j].ToString("0.00") + "\t");
            }

            sw.WriteLine();
        }
    }

    public void Clear()
    {
        for (int i = 0; i < Size; i++)
        {
            Di[i] = 0.0;

            for (int k = Ig[i]; k < Ig[i + 1]; k++)
            {
                Gg[k] = 0.0;
            }
        }
    }
}

public class Matrix
{
    private readonly double[,] _storage;
    public int Size { get; }

    public double this[int i, int j]
    {
        get => _storage[i, j];
        set => _storage[i, j] = value;
    }

    public Matrix(int size)
    {
        _storage = new double[size, size];
        Size = size;
    }

    public void Clear() => Array.Clear(_storage, 0, _storage.Length);

    public void Copy(Matrix destination)
    {
        for (int i = 0; i < destination.Size; i++)
        {
            for (int j = 0; j < destination.Size; j++)
            {
                destination[i, j] = _storage[i, j];
            }
        }
    }

    public static Matrix operator +(Matrix fstMatrix, Matrix sndMatrix)
    {
        Matrix resultMatrix = new(fstMatrix.Size);

        for (int i = 0; i < resultMatrix.Size; i++)
        {
            for (int j = 0; j < resultMatrix.Size; j++)
            {
                resultMatrix[i, j] = fstMatrix[i, j] + sndMatrix[i, j];
            }
        }

        return resultMatrix;
    }

    public static Matrix operator *(double value, Matrix matrix)
    {
        Matrix resultMatrix = new(matrix.Size);

        for (int i = 0; i < resultMatrix.Size; i++)
        {
            for (int j = 0; j < resultMatrix.Size; j++)
            {
                resultMatrix[i, j] = value * matrix[i, j];
            }
        }

        return resultMatrix;
    }
}