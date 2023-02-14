namespace Project;

public abstract class BaseMatrixAssembler
{
    protected readonly IBasis _basis;
    protected readonly IBaseMesh _mesh;
    protected readonly Integration _integrator;
    protected Matrix[]? _baseStiffnessMatrix;
    protected Matrix? _baseMassMatrix;

    public SparseMatrix? GlobalMatrix { get; set; } // need initialize with portrait builder 
    public Matrix StiffnessMatrix { get; }
    public Matrix MassMatrix { get; }
    public int BasisSize => _basis.Size;

    protected BaseMatrixAssembler(IBasis basis, Integration integrator, IBaseMesh mesh)
    {
        _basis = basis;
        _integrator = integrator;
        _mesh = mesh;
        StiffnessMatrix = new(_basis.Size);
        MassMatrix = new(_basis.Size);
    }

    public abstract void BuildLocalMatrices(int ielem);

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
    public BiMatrixAssembler(IBasis basis, Integration integrator, IBaseMesh mesh) : base(basis, integrator, mesh)
    {
    }

    public override void BuildLocalMatrices(int ielem)
    {
        var element = _mesh.Elements[ielem];
        var bPoint = _mesh.Points[element[0]];
        var ePoint = _mesh.Points[element[^1]];

        double hx = ePoint.X - bPoint.X;
        double hy = ePoint.Y - bPoint.Y;

        if (_baseStiffnessMatrix is null)
        {
            _baseStiffnessMatrix = new Matrix[] { new(_basis.Size), new(_basis.Size) };
            _baseMassMatrix = new(_basis.Size);
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
                MassMatrix[i, j] = MassMatrix[j, i] = hx * hy * _baseMassMatrix![i, j];
            }
        }
    }
}

public class CurveMatrixAssembler : BaseMatrixAssembler // maybe rename the class
{
    public CurveMatrixAssembler(IBasis basis, Integration integrator, IBaseMesh mesh) : base(basis, integrator, mesh)
    {
        _baseStiffnessMatrix = new Matrix[] { new(_basis.Size), new(_basis.Size) };
        _baseMassMatrix = new(_basis.Size);
    }

    public override void BuildLocalMatrices(int ielem)
    {
        var templateElement = new Rectangle(new(0.0, 0.0), new(1.0, 1.0));

        for (int i = 0; i < _basis.Size; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                var i1 = i;
                var j1 = j;
                Func<Point2D, double> function = p =>
                {
                    var dxFi1 = _basis.GetDPsi(i1, 0, p);
                    var dxFi2 = _basis.GetDPsi(j1, 0, p);
                    var dyFi1 = _basis.GetDPsi(i1, 1, p);
                    var dyFi2 = _basis.GetDPsi(j1, 1, p);
                    var calculates = CalculateJacobian(ielem, p);

                    return ((calculates.Reverse[0, 0] * dxFi1 + calculates.Reverse[0, 1] * dyFi1) *
                            (calculates.Reverse[0, 0] * dxFi2 + calculates.Reverse[0, 1] * dyFi2) +
                            (calculates.Reverse[1, 0] * dxFi1 + calculates.Reverse[1, 1] * dyFi1) *
                            (calculates.Reverse[1, 0] * dxFi2 + calculates.Reverse[1, 1] * dyFi2)) *
                           Math.Abs(calculates.Determinant);
                };

                _baseStiffnessMatrix![0][i, j] =
                    _baseStiffnessMatrix[0][j, i] = _integrator.Gauss2D(function, templateElement);

                function = p =>
                {
                    var fi1 = _basis.GetPsi(i1, p);
                    var fi2 = _basis.GetPsi(j1, p);
                    var calculates = CalculateJacobian(ielem, p);

                    return fi1 * fi2 * Math.Abs(calculates.Determinant);
                };
                _baseMassMatrix![i, j] = _baseMassMatrix[j, i] =
                    _integrator.Gauss2D(function, templateElement);
            }
        }

        for (int i = 0; i < _basis.Size; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                StiffnessMatrix[i, j] = StiffnessMatrix[j, i] = _baseStiffnessMatrix![0][i, j];
            }
        }

        for (int i = 0; i < _basis.Size; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                MassMatrix[i, j] = MassMatrix[j, i] = _baseMassMatrix![i, j];
            }
        }
    }

    private (double Determinant, Matrix Reverse) CalculateJacobian(int ielem, Point2D point)
    {
        var dx = new double[2];
        var dy = new double[2];

        var element = _mesh.Elements[ielem];

        for (int i = 0; i < _basis.Size; i++)
        {
            for (int k = 0; k < 2; k++)
            {
                dx[k] += _basis.GetDPsi(i, k, point) * _mesh.Points[element[i]].X;
                dy[k] += _basis.GetDPsi(i, k, point) * _mesh.Points[element[i]].Y;
            }
        }

        var jacobian = dx[0] * dy[1] - dx[1] * dy[0];

        var reverse = new Matrix(2)
        {
            [0, 0] = dy[1],
            [0, 1] = -dy[0],
            [1, 0] = -dx[1],
            [1, 1] = dx[0]
        };

        return (jacobian, 1.0 / jacobian * reverse);
    }
}