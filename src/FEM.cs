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

        public SolverFemBuilder SetDirichletBoundaries(DirichletBoundary[] boundaries)
        {
            _solverFem._dirichletBoundaries = boundaries;
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
    private DirichletBoundary[] _dirichletBoundaries = default!;
    private Matrix? _baseStiffnessMatrix = default!;
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
        _globalMatrix.PrintDense("matrix.txt");
        AccountingDirichletBoundary();

        // _iterativeSolver.SetMatrix(_globalMatrix);
        // _iterativeSolver.SetVector(_globalVector);
        // _iterativeSolver.Compute();
        //
        // foreach (var value in _iterativeSolver.Solution!)
        // {
        //     Console.WriteLine(value);
        // }
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
        var ePoint = _mesh.Points[element[_basis.Size - 1]];

        double hx = ePoint.X - bPoint.X;
        double hy = ePoint.Y - bPoint.Y;

        if (_baseStiffnessMatrix is null)
        {
            _baseStiffnessMatrix = new(_basis.Size);
            _baseMassMatrix = new(_basis.Size);
            var templateElement = new Rectangle(new(0.0, 0.0), new(1.0, 1.0));

            for (int i = 0; i < _basis.Size; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    var ik = i;
                    var jk = j;
                    Func<Point2D, double> function = p =>
                    {
                        var dXfi1 = _basis.GetDPsi(ik, 0, p);
                        var dXfi2 = _basis.GetDPsi(jk, 0, p);

                        var dYfi1 = _basis.GetDPsi(ik, 1, p);
                        var dYfi2 = _basis.GetDPsi(jk, 1, p);

                        return dXfi1 * dXfi2 + dYfi1 * dYfi2;
                    };

                    _baseStiffnessMatrix[i, j] = _baseStiffnessMatrix[j, i] =
                        _integrator.Gauss2D(function, templateElement);

                    function = p =>
                    {
                        var fi1 = _basis.GetPsi(ik, p);
                        var fi2 = _basis.GetPsi(jk, p);

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
                _stiffnessMatrix[i, j] = _stiffnessMatrix[j, i] = hy / hx * _baseStiffnessMatrix[i, j];
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

        if (i < j)
        {
            for (int ind = _globalMatrix.Ig[j]; ind < _globalMatrix.Ig[j + 1]; ind++)
            {
                if (_globalMatrix.Jg[ind] != i) continue;
                _globalMatrix.GGu[ind] += value;
                return;
            }
        }
        else
        {
            for (int ind = _globalMatrix.Ig[i]; ind < _globalMatrix.Ig[i + 1]; ind++)
            {
                if (_globalMatrix.Jg[ind] != j) continue;
                _globalMatrix.GGl[ind] += value;
                return;
            }
        }
    }

    private void AccountingDirichletBoundary()
    {
        (int Node, double Value)[] boundaries = new (int, double)[3 * _dirichletBoundaries.Length];
        int[] checkBc = new int[_mesh.Points.Count];

        int index = 0;

        foreach (var (ielem, edge) in _dirichletBoundaries)
        {
            for (int j = 0; j < 3; j++)
            {
                boundaries[index++] = (_mesh.Edges[ielem][edge][j],
                    _test.U(_mesh.Points[_mesh.Edges[ielem][edge][j]]));
            }
        }

        boundaries = boundaries.Distinct().OrderBy(boundary => boundary.Node).ToArray();
        checkBc.Fill(-1);

        for (int i = 0; i < boundaries.Length; i++)
            checkBc[boundaries[i].Node] = i;

        for (int i = 0; i < _mesh.Points.Count; i++)
        {
            if (checkBc[i] != -1)
            {
                _globalMatrix.Di[i] = 1;
                _globalVector[i] = boundaries[checkBc[i]].Value;

                for (int k = _globalMatrix.Ig[i]; k < _globalMatrix.Ig[i + 1]; k++)
                {
                    index = _globalMatrix.Jg[k];

                    if (checkBc[index] == -1)
                    {
                        _globalVector[index] -= _globalMatrix.GGl[k] * _globalVector[i];
                    }

                    _globalMatrix.GGl[k] = 0.0;
                    _globalMatrix.GGu[k] = 0.0;
                }
            }
            else
            {
                for (int k = _globalMatrix.Ig[i]; k < _globalMatrix.Ig[i + 1]; k++)
                {
                    index = _globalMatrix.Jg[k];

                    if (checkBc[index] == -1) continue;
                    _globalVector[i] -= _globalMatrix.GGu[k] * _globalVector[index];
                    _globalMatrix.GGu[k] = 0.0;
                    _globalMatrix.GGl[k] = 0.0;
                }
            }
        }
    }

    public static SolverFemBuilder CreateBuilder() => new();
}