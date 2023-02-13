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

        public SolverFemBuilder SetAssembler(BaseMatrixAssembler matrixAssembler)
        {
            _solverFem._matrixAssembler = matrixAssembler;
            return this;
        }

        public static implicit operator SolverFem(SolverFemBuilder builder)
            => builder._solverFem;
    }

    private IBaseMesh _mesh = default!;
    private ITest _test = default!;
    private IterativeSolver _iterativeSolver = default!;
    private IEnumerable<IBoundary> _boundaries = default!;
    private Vector<double> _localVector = default!;
    private Vector<double> _globalVector = default!;
    private BaseMatrixAssembler _matrixAssembler = default!;

    public void Compute()
    {
        Initialize();
        AssemblySystem();
        AccountingDirichletBoundary();

        _iterativeSolver.SetMatrix(_matrixAssembler.GlobalMatrix!);
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
        PortraitBuilder.Build(_mesh, out var ig, out var jg);
        _matrixAssembler.GlobalMatrix = new(ig.Length - 1, jg.Length)
        {
            Ig = ig,
            Jg = jg
        };

        _globalVector = new(ig.Length - 1);
        _localVector = new(_matrixAssembler.BasisSize);
    }

    private void AssemblySystem()
    {
        for (int ielem = 0; ielem < _mesh.Elements.Count; ielem++)
        {
            var element = _mesh.Elements[ielem];
            
            _matrixAssembler.BuildLocalMatrices(ielem);
            BuildLocalVector(ielem);

            for (int i = 0; i < _matrixAssembler.BasisSize; i++)
            {
                _globalVector[element[i]] += _localVector[i];

                for (int j = 0; j < _matrixAssembler.BasisSize; j++)
                {
                    _matrixAssembler.FillGlobalMatrix(element[i], element[j], _matrixAssembler.StiffnessMatrix[i, j]);
                }
            }
        }
    }

    private void BuildLocalVector(int ielem)
    {
        _localVector.Fill(0.0);

        for (int i = 0; i < _matrixAssembler.BasisSize; i++)
        {
            for (int j = 0; j < _matrixAssembler.BasisSize; j++)
            {
                _localVector[i] += _matrixAssembler.MassMatrix[i, j] * _test.F(_mesh.Points[_mesh.Elements[ielem][j]]);
            }
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
                _matrixAssembler.GlobalMatrix!.Di[i] = 1.0;
                _globalVector[i] = arrayBoundaries[checkBc[i]].Value;

                for (int k = _matrixAssembler.GlobalMatrix.Ig[i]; k < _matrixAssembler.GlobalMatrix.Ig[i + 1]; k++)
                {
                    index = _matrixAssembler.GlobalMatrix.Jg[k];

                    if (checkBc[index] == -1)
                    {
                        _globalVector[index] -= _matrixAssembler.GlobalMatrix.Gg[k] * _globalVector[i];
                    }

                    _matrixAssembler.GlobalMatrix.Gg[k] = 0.0;
                }
            }
            else
            {
                for (int k = _matrixAssembler.GlobalMatrix!.Ig[i]; k < _matrixAssembler.GlobalMatrix.Ig[i + 1]; k++)
                {
                    index = _matrixAssembler.GlobalMatrix.Jg[k];

                    if (checkBc[index] == -1) continue;
                    _globalVector[i] -= _matrixAssembler.GlobalMatrix.Gg[k] * _globalVector[index];
                    _matrixAssembler.GlobalMatrix.Gg[k] = 0.0;
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