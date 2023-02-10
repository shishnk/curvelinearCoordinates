namespace Project;

public class SolverFem
{
    public class SolverFemBuilder
    {
        private readonly SolverFem _solverFem = new();

        public SolverFemBuilder SetTest(ITest test)
        {
            _solverFem._test = test;
            return this;
        }

        public SolverFemBuilder SetMesh(IBaseMesh mesh)
        {
            _solverFem._mesh = mesh;
            return this;
        }

        public SolverFemBuilder SetSolverSlae(IterativeSolver iterativeSolver)
        {
            _solverFem._iterativeSolver = iterativeSolver;
            return this;
        }

        public SolverFemBuilder SetBoundaries(IEnumerable<IBoundary> boundaries)
        {
            _solverFem._boundaries = boundaries.DistinctBy(b => b.Node);
            return this;
        }

        public SolverFemBuilder SetIntegrator(Integration integrator)
        {
            _solverFem._integrator = integrator;
            return this;
        }

        public SolverFemBuilder SetBasis(IBasis basis)
        {
            _solverFem._basis = basis;
            return this;
        }

        public static implicit operator SolverFem(SolverFemBuilder builder)
            => builder._solverFem;
    }

    private IBaseMesh _mesh = default!;
    private ITest _test = default!;
    private Integration _integrator = default!;
    private IterativeSolver _iterativeSolver = default!;
    private IEnumerable<IBoundary> _boundaries = default!;
    private Matrix[]? _baseStiffnessMatrix;
    private Matrix _baseMassMatrix = default!;
    private Matrix _stiffnessMatrix = default!;
    private Matrix _massMatrix = default!;
    private SparseMatrix _globalMatrix = default!;
    private Vector<double> _localVector = default!;
    private Vector<double> _globalVector = default!;
    private IBasis _basis = default!;

    public void Compute()
    {
         Initialize();
        AssemblySystem();
        AccountingDirichletBoundary();

        _globalMatrix.PrintDense("matrix.txt");

        _iterativeSolver.SetMatrix(_globalMatrix);
        _iterativeSolver.SetVector(_globalVector);
        _iterativeSolver.Compute();

        var exact = new double[_mesh.Points.Count];

        for (int i = 0; i < exact.Length; i++)
        {
            exact[i] = _test.U(_mesh.Points[i]);
        }

        var result = exact.Zip(_iterativeSolver.Solution!.Value, (v1, v2) => (v1, v2));

        foreach (var (v1, v2) in result)
        {
            Console.WriteLine($"{v1} ------------ {v2} ");
        }

        Console.WriteLine("---------------------------");

        CalculateError();
    }

    private void Initialize()
    {
        _stiffnessMatrix = new(_basis.Size);
        _massMatrix = new(_basis.Size);

        PortraitBuilder.Build(_mesh, out var ig, out var jg);
        _globalMatrix = new(ig.Length - 1, jg.Length)
        {
            Ig = ig,
            Jg = jg
        };

        _globalVector = new(ig.Length - 1);
        _localVector = new(_basis.Size);
    }

    private void AssemblySystem()
    {
        for (int ielem = 0; ielem < _mesh.Elements.Count; ielem++)
        {
            var element = _mesh.Elements[ielem];

            BuildLocalMatrices(ielem);
            BuildLocalVector(ielem);

            for (int i = 0; i < _basis.Size; i++)
            {
                _globalVector[element[i]] += _localVector[i];

                for (int j = 0; j < _basis.Size; j++)
                {
                    FillGlobalMatrix(element[i], element[j], _stiffnessMatrix[i, j]);
                }
            }
        }
    }

    private void BuildLocalMatrices(int ielem)
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
                _stiffnessMatrix[i, j] = _stiffnessMatrix[j, i] =
                    hy / hx * _baseStiffnessMatrix[0][i, j] + hx / hy * _baseStiffnessMatrix[1][i, j];
            }
        }

        for (int i = 0; i < _basis.Size; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                _massMatrix[i, j] = _massMatrix[j, i] = hx * hy * _baseMassMatrix[i, j];
            }
        }
    }

    private void BuildLocalVector(int ielem)
    {
        _localVector.Fill(0.0);

        for (int i = 0; i < _basis.Size; i++)
        {
            for (int j = 0; j < _basis.Size; j++)
            {
                _localVector[i] += _massMatrix[i, j] * _test.F(_mesh.Points[_mesh.Elements[ielem][j]]);
            }
        }
    }

    private void FillGlobalMatrix(int i, int j, double value)
    {
        if (i == j)
        {
            _globalMatrix.Di[i] += value;
            return;
        }

        if (i <= j) return;
        for (int ind = _globalMatrix.Ig[i]; ind < _globalMatrix.Ig[i + 1]; ind++)
        {
            if (_globalMatrix.Jg[ind] != j) continue;
            _globalMatrix.Gg[ind] += value;
            return;
        }
    }

    private void AccountingDirichletBoundary()
    {
        int[] checkBc = new int[_mesh.Points.Count];

        checkBc.Fill(-1);
        var arrayBoundaries = _boundaries.ToArray();

        for (int i = 0; i < arrayBoundaries.Length; i++)
        {
            arrayBoundaries[i].Value = _test.U(_mesh.Points[arrayBoundaries[i].Node]);
            checkBc[arrayBoundaries[i].Node] = i;
        }

        for (int i = 0; i < _mesh.Points.Count; i++)
        {
            int index;
            if (checkBc[i] != -1)
            {
                _globalMatrix.Di[i] = 1.0;
                _globalVector[i] = arrayBoundaries[checkBc[i]].Value;

                for (int k = _globalMatrix.Ig[i]; k < _globalMatrix.Ig[i + 1]; k++)
                {
                    index = _globalMatrix.Jg[k];

                    if (checkBc[index] == -1)
                    {
                        _globalVector[index] -= _globalMatrix.Gg[k] * _globalVector[i];
                    }

                    _globalMatrix.Gg[k] = 0.0;
                }
            }
            else
            {
                for (int k = _globalMatrix.Ig[i]; k < _globalMatrix.Ig[i + 1]; k++)
                {
                    index = _globalMatrix.Jg[k];

                    if (checkBc[index] == -1) continue;
                    _globalVector[i] -= _globalMatrix.Gg[k] * _globalVector[index];
                    _globalMatrix.Gg[k] = 0.0;
                }
            }
        }
    }

    private void CalculateError()
    {
        var error = new double[_mesh.Points.Count];

        for (int i = 0; i < error.Length; i++)
        {
            error[i] = Math.Abs(_iterativeSolver.Solution!.Value[i] - _test.U(_mesh.Points[i]));
        }

        Array.ForEach(error, Console.WriteLine);
    }

    public static SolverFemBuilder CreateBuilder() => new();
}