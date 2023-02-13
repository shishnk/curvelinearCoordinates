var meshParameters = MeshParameters.ReadJson("input/meshParameters.json");
var boundariesParameters = BoundaryParameters.ReadJson("input/boundaryParameters.json");
var boundaryHandler = new QuadraticBoundaryHandler(boundariesParameters, meshParameters);
var meshCreator = new RegularMeshCreator();
var mesh = meshCreator.CreateMesh(meshParameters, new MeshQuadraticBuilder());
SolverFem problem = SolverFem.CreateBuilder()
    .SetMesh(mesh)
    .SetTest(new Test2())
    .SetSolverSlae(new CGMCholesky(1000, 1E-15))
    .SetAssembler(new CurvedMatrixAssembler(new QuadraticBasis(), new(Quadratures.SegmentGaussOrder5()), mesh))
    .SetBoundaries(boundaryHandler.Process());

problem.Compute();