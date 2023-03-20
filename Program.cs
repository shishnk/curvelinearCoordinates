var meshParameters = CurveMeshParameters.ReadJson("input/curveMeshParameters.jsonc");
// var meshParameters = MeshParameters.ReadJson("input/meshParameters.json");
var boundariesParameters = BoundaryParameters.ReadJson("input/boundaryParameters.json");
var boundaryHandler = new CurveQuadraticBoundaryHandler(boundariesParameters, meshParameters);
var meshCreator = new RegularMeshCreator();
var mesh = meshCreator.CreateMesh(meshParameters, new CurveQuadraticMeshBuilder());
SolverFem problem = SolverFem.CreateBuilder()
    .SetMesh(mesh)
    .SetTest(new Test3())
    .SetSolverSlae(new CGMCholesky(1000, 1E-16))
    .SetAssembler(new CurveMatrixAssembler(new QuadraticBasis(), new(Quadratures.SegmentGaussOrder5()), mesh,
        false))
    .SetBoundaries(boundaryHandler.Process());

problem.Compute();
// problem.DoResearch();
// problem.CalculateAtPoint((0.12, 2.81), true);
// problem.CalculateAtPoint((2.0, 0.1), true);
problem.Integrate();