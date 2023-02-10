var parameters = MeshParameters.ReadJson("input/meshParameters.json");
var boundaries = DirichletBoundary.ReadJson("input/DirichletBoundaries.json");
var meshCreator = new RegularMeshCreator();
var mesh = meshCreator.CreateMesh(parameters, new MeshQuadraticBuilder());
SolverFem problem = SolverFem.CreateBuilder()
    .SetBasis(new QuadraticBasis())
    .SetIntegrator(new(Quadratures.SegmentGaussOrder5()))
    .SetMesh(mesh)
    .SetTest(new Test1())
    .SetSolverSlae(new CGMCholesky(1000, 1E-13))
    .SetDirichletBoundaries(boundaries);

problem.Compute();