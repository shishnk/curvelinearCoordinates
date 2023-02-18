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
    private readonly BoundaryParameters? _parameters;
    private readonly MeshParameters? _meshParameters;

    public LinearBoundaryHandler(BoundaryParameters? parameters, IParameters? meshParameters)
        => (_parameters, _meshParameters) = (parameters,
            (MeshParameters)(meshParameters ?? throw new ArgumentNullException(nameof(meshParameters))));

    public IEnumerable<IBoundary> Process() // for now only Dirichlet
    {
        if (_parameters!.Value.TopBorder == 1)
        {
            int startingNode = (_meshParameters!.Value.SplitsX + 1) * _meshParameters.Value.SplitsY;

            for (int i = 0; i < _meshParameters.Value.SplitsX + 1; i++)
            {
                yield return new DirichletBoundary(startingNode + i, 0.0);
            }
        }

        if (_parameters.Value.BottomBorder == 1)
        {
            for (int i = 0; i < _meshParameters!.Value.SplitsX + 1; i++)
            {
                yield return new DirichletBoundary(i, 0.0);
            }
        }

        if (_parameters.Value.LeftBorder == 1)
        {
            for (int i = 0; i < _meshParameters!.Value.SplitsY + 1; i++)
            {
                yield return new DirichletBoundary(i * (_meshParameters.Value.SplitsX + 1), 0.0);
            }
        }

        if (_parameters.Value.RightBorder != 1) yield break;
        {
            for (int i = 0; i < _meshParameters!.Value.SplitsY + 1; i++)
            {
                yield return new DirichletBoundary(
                    i * _meshParameters.Value.SplitsX + _meshParameters.Value.SplitsX + i, 0.0);
            }
        }
    }
}

public class QuadraticBoundaryHandler : IBoundaryHandler
{
    private readonly BoundaryParameters? _parameters;
    private readonly MeshParameters? _meshParameters;

    public QuadraticBoundaryHandler(BoundaryParameters? parameters, IParameters meshParameters)
        => (_parameters, _meshParameters) = (parameters, (MeshParameters)meshParameters);

    public IEnumerable<IBoundary> Process() // for now only Dirichlet
    {
        if (_parameters!.Value.TopBorder == 1)
        {
            int startingNode = (2 * _meshParameters!.Value.SplitsX + 1) * 2 * _meshParameters.Value.SplitsY;

            for (int i = 0; i < 2 * _meshParameters.Value.SplitsX + 1; i++)
            {
                yield return new DirichletBoundary(startingNode + i, 0.0);
            }
        }

        if (_parameters.Value.BottomBorder == 1)
        {
            for (int i = 0; i < 2 * _meshParameters!.Value.SplitsX + 1; i++)
            {
                yield return new DirichletBoundary(i, 0.0);
            }
        }

        if (_parameters.Value.LeftBorder == 1)
        {
            for (int i = 0; i < 2 * _meshParameters!.Value.SplitsY + 1; i++)
            {
                yield return new DirichletBoundary(i * (2 * _meshParameters.Value.SplitsX + 1), 0.0);
            }
        }

        if (_parameters.Value.RightBorder != 1) yield break;
        {
            for (int i = 0; i < 2 * _meshParameters!.Value.SplitsY + 1; i++)
            {
                yield return new DirichletBoundary(
                    i * 2 * _meshParameters.Value.SplitsX + 2 * _meshParameters.Value.SplitsX + i, 0.0);
            }
        }
    }
}

public class CurveLinearBoundaryHandler : IBoundaryHandler
{
    private readonly BoundaryParameters? _parameters;
    private readonly CurveMeshParameters? _meshParameters;

    public CurveLinearBoundaryHandler(BoundaryParameters? parameters, IParameters? meshParameters)
        => (_parameters, _meshParameters) = (parameters,
            (CurveMeshParameters)(meshParameters ?? throw new ArgumentNullException(nameof(meshParameters))));

    public IEnumerable<IBoundary> Process() // for now only Dirichlet
    {
        var array = new DirichletBoundary[2 * _meshParameters!.Value.Steps];

        for (int i = 0,
             k = _meshParameters.Value.Steps,
             j = _meshParameters.Value.Splits != 0
                 ? 2 * _meshParameters.Value.Splits *
                   _meshParameters!.Value.Steps
                 : _meshParameters.Value.Steps;
             i < array.Length / 2;
             i++, j++, k++)
        {
            array[i] = new(i, 0.0);
            array[k] = new(j, 0.0);
        }

        return array;
    }
}