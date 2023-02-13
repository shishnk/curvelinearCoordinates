var meshParameters = CurveMeshParameters.ReadJson("input/curveMeshParameters.json");

// var meshParameters = MeshParameters.ReadJson("input/meshParameters.json");
var boundariesParameters = BoundaryParameters.ReadJson("input/boundaryParameters.json");
var boundaryHandler = new CurveLinearBoundaryHandler(meshParameters, boundariesParameters);
var meshCreator = new RegularMeshCreator();
var mesh = meshCreator.CreateMesh(meshParameters, new CurveLinearMeshBuilder());
SolverFem problem = SolverFem.CreateBuilder()
    .SetMesh(mesh)
    .SetTest(new Test2())
    .SetSolverSlae(new CGMCholesky(1000, 1E-15))
    .SetAssembler(new CurveMatrixAssembler(new LinearBasis(), new(Quadratures.SegmentGaussOrder5()), mesh))
    .SetBoundaries(boundaryHandler.Process());

problem.Compute();