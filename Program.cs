var parameters = MeshParameters.ReadJson("input/meshParameters.json");
var meshCreator = new RegularMeshCreator();
var mesh = meshCreator.CreateMesh(parameters, new MeshQuadraticBuilder());
