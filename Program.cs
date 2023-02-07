IMeshCreator meshCreator = new RegularMeshCreator();
var parameters = MeshParameters.ReadJson(@"C:\Users\Hukutka\source\repos\Project\input\meshParameters.json");
var mesh = meshCreator.CreateMesh(parameters, new MeshQuadraticBuilder());