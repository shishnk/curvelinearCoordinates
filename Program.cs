var meshParameters = MeshParameters.ReadJson("input/meshParameters.json");
var boundariesParameters = BoundaryParameters.ReadJson("input/boundaryParameters.json");
var boundaryHandler = new LinearBoundaryHandler(boundariesParameters, meshParameters);
var meshCreator = new RegularMeshCreator();
var mesh = meshCreator.CreateMesh(meshParameters, new MeshLinearBuilder());
SolverFem problem = SolverFem.CreateBuilder()
    .SetBasis(new LinearBasis())
    .SetIntegrator(new(Quadratures.SegmentGaussOrder5()))
    .SetMesh(mesh)
    .SetTest(new Test1())
    .SetSolverSlae(new CGMCholesky(1000, 1E-15))
    .SetBoundaries(boundaryHandler.Process());

problem.Compute();