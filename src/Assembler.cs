namespace Project;

public abstract class BaseMatrixAssembler
{
    protected readonly IBasis _basis;
    protected readonly Integration _integrator;
    protected readonly Matrix[] _baseStiffnessMatrix;
    protected readonly Matrix _baseMassMatrix;

    public SparseMatrix? GlobalMatrix { get; set; } // need initialize with portrait builder 
    public Matrix StiffnessMatrix { get; }
    public Matrix MassMatrix { get; }
    public int BasisSize => _basis.Size;

    protected BaseMatrixAssembler(IBasis basis, Integration integrator)
    {
        _basis = basis;
        _baseStiffnessMatrix = new Matrix[] { new(_basis.Size), new(_basis.Size) };
        _baseMassMatrix = new(_basis.Size);
        StiffnessMatrix = new(_basis.Size);
        MassMatrix = new(_basis.Size);
        _integrator = integrator;
    }

    public abstract void BuildLocalMatrices(double hx, double hy);

    public void FillGlobalMatrix(int i, int j, double value)
    {
        if (GlobalMatrix is null)
        {
            throw new("Initialize the global matrix (use portrait builder)!");
        }

        if (i == j)
        {
            GlobalMatrix.Di[i] += value;
            return;
        }

        if (i <= j) return;
        for (int ind = GlobalMatrix.Ig[i]; ind < GlobalMatrix.Ig[i + 1]; ind++)
        {
            if (GlobalMatrix.Jg[ind] != j) continue;
            GlobalMatrix.Gg[ind] += value;
            return;
        }
    }
}

public class BiMatrixAssembler : BaseMatrixAssembler
{
    public BiMatrixAssembler(IBasis basis, Integration integrator) : base(basis, integrator)
    {
    }

    public override void BuildLocalMatrices(double hx, double hy)
    {
        var templateElement = new Rectangle(new(0.0, 0.0), new(1.0, 1.0));

        for (int i = 0; i < _basis.Size; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                Func<Point2D, double> function;

                for (int k = 0; k < 2; k++)
                {
                    var ik = i;
                    var jk = j;
                    var k1 = k;
                    function = p =>
                    {
                        var dFi1 = _basis.GetDPsi(ik, k1, p);
                        var dFi2 = _basis.GetDPsi(jk, k1, p);

                        return dFi1 * dFi2;
                    };

                    _baseStiffnessMatrix[k][i, j] = _baseStiffnessMatrix[k][j, i] =
                        _integrator.Gauss2D(function, templateElement);
                }

                var i1 = i;
                var j1 = j;
                function = p =>
                {
                    var fi1 = _basis.GetPsi(i1, p);
                    var fi2 = _basis.GetPsi(j1, p);

                    return fi1 * fi2;
                };
                _baseMassMatrix[i, j] = _baseMassMatrix[j, i] = _integrator.Gauss2D(function, templateElement);
            }
        }

        for (int i = 0; i < _basis.Size; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                StiffnessMatrix[i, j] = StiffnessMatrix[j, i] =
                    hy / hx * _baseStiffnessMatrix[0][i, j] + hx / hy * _baseStiffnessMatrix[1][i, j];
            }
        }

        for (int i = 0; i < _basis.Size; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                MassMatrix[i, j] = MassMatrix[j, i] = hx * hy * _baseMassMatrix[i, j];
            }
        }
    }
}

// public class CurvedMatrixAssembler : BaseMatrixAssembler // maybe go change name of class
// {
//     public override void BuildLocalMatrices()
//     {
//         throw new NotImplementedException();
//     }
// }