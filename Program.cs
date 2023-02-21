var meshParameters = CurveMeshParameters.ReadJson("input/curveMeshParameters.json");
// var meshParameters = MeshParameters.ReadJson("input/meshParameters.json");
var boundariesParameters = BoundaryParameters.ReadJson("input/boundaryParameters.json");
var boundaryHandler = new CurveQuadraticBoundaryHandler(boundariesParameters, meshParameters);
var meshCreator = new RegularMeshCreator();
var mesh = meshCreator.CreateMesh(meshParameters, new CurveQuadraticMeshBuilder());
SolverFem problem = SolverFem.CreateBuilder()
    .SetMesh(mesh)
    .SetTest(new Test1())
    .SetSolverSlae(new CGMCholesky(1000, 1E-15))
    .SetAssembler(new CurveMatrixAssembler(new QuadraticBasis(), new(Quadratures.SegmentGaussOrder5()), mesh))
    .SetBoundaries(boundaryHandler.Process());

problem.Compute();