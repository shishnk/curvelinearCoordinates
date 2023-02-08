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
    private Matrix _stiffnessMatrix = default!;
    private Matrix _massMatrix = default!;
    private SparseMatrix _globalMatrix = default!;
    private Vector<double> _globalVector = default!;
    private IBasis _basis = default!;

    public void Compute()
    {
        Initialize();
        AssemblySystem();
        AccountingDirichletBoundary();

        _iterativeSolver.SetMatrix(_globalMatrix);
        _iterativeSolver.SetVector(_globalVector);
        _iterativeSolver.Compute();

        foreach (var value in _iterativeSolver.Solution!)
        {
            Console.WriteLine(value);
        }
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
    }

    private void AssemblySystem()
    {
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
    }
}