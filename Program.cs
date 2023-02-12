var meshParameters = MeshParameters.ReadJson("input/meshParameters.json");
var boundariesParameters = BoundaryParameters.ReadJson("input/boundaryParameters.json");
var boundaryHandler = new QuadraticBoundaryHandler(boundariesParameters, meshParameters);
var meshCreator = new RegularMeshCreator();
var mesh = meshCreator.CreateMesh(meshParameters, new MeshQuadraticBuilder());
SolverFem problem = SolverFem.CreateBuilder()
    .SetMesh(mesh)
    .SetTest(new Test1())
    .SetSolverSlae(new CGMCholesky(1000, 1E-15))
    .SetAssembler(new BiMatrixAssembler(new QuadraticBasis(), new(Quadratures.SegmentGaussOrder5())))
    .SetBoundaries(boundaryHandler.Process());

problem.Compute();