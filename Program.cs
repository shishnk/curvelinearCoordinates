var meshParameters = MeshParameters.ReadJson("input/meshParameters.json");
var boundariesParameters = BoundaryParameters.ReadJson("input/boundaryParameters.json");
var boundaryHandler = new BoundaryHandler(boundariesParameters, meshParameters);
var meshCreator = new RegularMeshCreator();
var mesh = meshCreator.CreateMesh(meshParameters, new MeshQuadraticBuilder());
SolverFem problem = SolverFem.CreateBuilder()
    .SetBasis(new QuadraticBasis())
    .SetIntegrator(new(Quadratures.SegmentGaussOrder5()))
    .SetMesh(mesh)
    .SetTest(new Test1())
    .SetSolverSlae(new CGMCholesky(1000, 1E-13))
    .SetBoundaries(boundaryHandler.Process());

problem.Compute();