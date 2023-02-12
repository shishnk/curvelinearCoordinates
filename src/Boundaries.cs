namespace Project;

public interface IBoundary
{
    int Node { get; }
    double Value { get; set; }
}

public class DirichletBoundary : IBoundary
{
    public int Node { get; }
    public double Value { get; set; }

    public DirichletBoundary(int node, double value) => (Node, Value) = (node, value);
}

public readonly record struct BoundaryParameters
{
    public required byte LeftBorder { get; init; }
    public required byte RightBorder { get; init; }
    public required byte BottomBorder { get; init; }
    public required byte TopBorder { get; init; }

    public static BoundaryParameters ReadJson(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new("File does not exist");
        }

        using var sr = new StreamReader(jsonPath);
        return JsonConvert.DeserializeObject<BoundaryParameters>(sr.ReadToEnd());
    }
}

public interface IBoundaryHandler
{
    IEnumerable<IBoundary> Process();
}

public class LinearBoundaryHandler : IBoundaryHandler
{
    private readonly BoundaryParameters _parameters;
    private readonly MeshParameters _meshParameters;

    public LinearBoundaryHandler(BoundaryParameters parameters, MeshParameters meshParameters)
        => (_parameters, _meshParameters) = (parameters, meshParameters);

    public IEnumerable<IBoundary> Process() // for now only Dirichlet
    {
        if (_parameters.TopBorder == 1)
        {
            int startingNode = (_meshParameters.SplitsX + 1) * _meshParameters.SplitsY;

            for (int i = 0; i < _meshParameters.SplitsX + 1; i++)
            {
                yield return new DirichletBoundary(startingNode + i, 0.0);
            }
        }

        if (_parameters.BottomBorder == 1)
        {
            for (int i = 0; i < _meshParameters.SplitsX + 1; i++)
            {
                yield return new DirichletBoundary(i, 0.0);
            }
        }

        if (_parameters.LeftBorder == 1)
        {
            for (int i = 0; i < _meshParameters.SplitsY + 1; i++)
            {
                yield return new DirichletBoundary(i * ( _meshParameters.SplitsX + 1), 0.0);
            }
        }

        if (_parameters.RightBorder != 1) yield break;
        {
            for (int i = 0; i < _meshParameters.SplitsY + 1; i++)
            {
                yield return new DirichletBoundary(
                    i *  _meshParameters.SplitsX + _meshParameters.SplitsX + i, 0.0);
            }
        }
    }
}

public class QuadraticBoundaryHandler : IBoundaryHandler
{
    private readonly BoundaryParameters _parameters;
    private readonly MeshParameters _meshParameters;

    public QuadraticBoundaryHandler(BoundaryParameters parameters, MeshParameters meshParameters)
        => (_parameters, _meshParameters) = (parameters, meshParameters);

    public IEnumerable<IBoundary> Process() // for now only Dirichlet
    {
        if (_parameters.TopBorder == 1)
        {
            int startingNode = (2 * _meshParameters.SplitsX + 1) * 2 * _meshParameters.SplitsY;

            for (int i = 0; i < 2 * _meshParameters.SplitsX + 1; i++)
            {
                yield return new DirichletBoundary(startingNode + i, 0.0);
            }
        }

        if (_parameters.BottomBorder == 1)
        {
            for (int i = 0; i < 2 * _meshParameters.SplitsX + 1; i++)
            {
                yield return new DirichletBoundary(i, 0.0);
            }
        }

        if (_parameters.LeftBorder == 1)
        {
            for (int i = 0; i < 2 * _meshParameters.SplitsY + 1; i++)
            {
                yield return new DirichletBoundary(i * (2 * _meshParameters.SplitsX + 1), 0.0);
            }
        }

        if (_parameters.RightBorder != 1) yield break;
        {
            for (int i = 0; i < 2 * _meshParameters.SplitsY + 1; i++)
            {
                yield return new DirichletBoundary(
                    i * 2 * _meshParameters.SplitsX + 2 * _meshParameters.SplitsX + i, 0.0);
            }
        }
    }
}