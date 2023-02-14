namespace Project.Meshes;

public interface IMeshCreator
{
    IBaseMesh CreateMesh(IParameters meshParameters, MeshBuilder? meshBuilder = null);
}

public class RegularMeshCreator : IMeshCreator
{
    public IBaseMesh CreateMesh(IParameters meshParameters, MeshBuilder? meshBuilder = null) =>
        new RegularMesh(meshParameters, meshBuilder ?? new LinearMeshBuilder());
}

// public class IrregularMesh : BaseMesh TODO maybe

public abstract class MeshBuilder
{
    protected abstract int SizeElement { get; }

    public abstract (List<Point2D>, int[][]) Build(IParameters meshParameters);

    protected (List<Point2D>, int[][]) BaseBuild(IParameters meshParameters)
    {
        if (meshParameters is not MeshParameters parameters)
        {
            throw new ArgumentNullException(nameof(parameters), "Parameters mesh is null!");
        }

        if (parameters.SplitsX is < 1 or < 1)
        {
            throw new("The number of splits must be greater than or equal to 1");
        }

        var result = new
        {
            Points = new List<Point2D>(),
            Elements = new int[parameters.SplitsX * parameters.SplitsY][].Select(_ => new int[SizeElement])
                .ToArray(),
        };

        double hx = parameters.IntervalX.Length / parameters.SplitsX;
        double hy = parameters.IntervalY.Length / parameters.SplitsY;

        double[] pointsX = new double[parameters.SplitsX + 1];
        double[] pointsY = new double[parameters.SplitsY + 1];

        pointsX[0] = parameters.IntervalX.LeftBorder;
        pointsY[0] = parameters.IntervalY.LeftBorder;

        for (int i = 1; i < parameters.SplitsX + 1; i++)
        {
            pointsX[i] = pointsX[i - 1] + hx;
        }

        for (int i = 1; i < parameters.SplitsY + 1; i++)
        {
            pointsY[i] = pointsY[i - 1] + hy;
        }

        for (int j = 0; j < parameters.SplitsY + 1; j++)
        {
            for (int i = 0; i < parameters.SplitsX + 1; i++)
            {
                result.Points.Add(new(pointsX[i], pointsY[j]));
            }
        }

        int nx = parameters.SplitsX + 1;
        int index = 0;

        for (int j = 0; j < parameters.SplitsY; j++)
        {
            for (int i = 0; i < parameters.SplitsX; i++)
            {
                result.Elements[index][0] = i + j * nx;
                result.Elements[index][1] = i + 1 + j * nx;
                result.Elements[index][2] = i + (j + 1) * nx;
                result.Elements[index++][3] = i + 1 + (j + 1) * nx;
            }
        }

        return (result.Points, result.Elements);
    }
}

public class LinearMeshBuilder : MeshBuilder
{
    protected override int SizeElement => 4;

    public override (List<Point2D>, int[][]) Build(IParameters meshParameters)
    {
        if (meshParameters is not MeshParameters parameters)
        {
            throw new ArgumentNullException(nameof(parameters), "Parameters mesh is null!");
        }

        var result = BaseBuild(meshParameters);

        return (result.Item1, result.Item2);
    }
}

public class QuadraticMeshBuilder : MeshBuilder
{
    protected override int SizeElement => 9;

    public override (List<Point2D>, int[][]) Build(IParameters meshParameters)
    {
        if (meshParameters is not MeshParameters parameters)
        {
            throw new ArgumentNullException(nameof(parameters), "Parameters mesh is null!");
        }

        (List<Point2D> Points, int[][] Elements) result = BaseBuild(meshParameters);
        var pointsX = new double[2 * parameters.SplitsX + 1];
        var pointsY = new double[2 * parameters.SplitsY + 1];
        var vertices = new Point2D[9];

        pointsX.Fill(int.MinValue);
        pointsY.Fill(int.MinValue);

        foreach (var ielem in result.Elements)
        {
            var v1 = result.Points[ielem[0]];
            var v2 = result.Points[ielem[1]];
            var v3 = result.Points[ielem[2]];
            var v4 = result.Points[ielem[3]];

            RecalculatePoints(v1, v2, v3, v4);

            pointsX = pointsX.Concat(vertices.Select(p => p.X)).ToArray();
            pointsY = pointsY.Concat(vertices.Select(p => p.Y)).ToArray();
        }

        pointsX = pointsX.OrderBy(v => v).Distinct().ToArray();
        pointsY = pointsY.OrderBy(v => v).Distinct().ToArray();
        result.Points.Clear();

        foreach (var pointY in pointsY.Skip(1))
        {
            foreach (var pointX in pointsX.Skip(1))
            {
                result.Points.Add(new(pointX, pointY));
            }
        }

        var nx = 2 * parameters.SplitsX + 1;
        var index = 0;

        for (int j = 0; j < parameters.SplitsY; j++)
        {
            for (int i = 0; i < parameters.SplitsX; i++)
            {
                result.Elements[index][0] = i + j * 2 * nx + i;
                result.Elements[index][1] = i + 1 + 2 * j * nx + i;
                result.Elements[index][2] = i + 2 + 2 * j * nx + i;
                result.Elements[index][3] = i + nx + 2 * j * nx + i;
                result.Elements[index][4] = i + nx + 1 + 2 * j * nx + i;
                result.Elements[index][5] = i + nx + 2 + 2 * j * nx + i;
                result.Elements[index][6] = i + 2 * nx + 2 * j * nx + i;
                result.Elements[index][7] = i + 2 * nx + 1 + 2 * j * nx + i;
                result.Elements[index++][8] = i + 2 * nx + 2 + 2 * j * nx + i;
            }
        }

        return (result.Points, result.Elements);

        void RecalculatePoints(Point2D v1, Point2D v2, Point2D v3, Point2D v4)
        {
            vertices[0] = v1;
            vertices[1] = v2;
            vertices[2] = v3;
            vertices[3] = v4;
            vertices[4] = (v1 + v2) / 2.0;
            vertices[5] = (v3 + v4) / 2.0;
            vertices[6] = (v1 + v3) / 2.0;
            vertices[7] = (v4 + v2) / 2.0;
            vertices[8] = (vertices[4] + vertices[5]) / 2.0;
        }
    }
}

public class CurveLinearMeshBuilder : MeshBuilder
{
    protected override int SizeElement => 4;

    public override (List<Point2D>, int[][]) Build(IParameters meshParameters)
    {
        if (meshParameters is not CurveMeshParameters parameters)
        {
            throw new ArgumentNullException(nameof(parameters), "Parameters mesh is null!");
        }

        var result = new
        {
            Points = new List<Point2D>(),
            Elements = new int[parameters.Steps == 4 ? 1 : parameters.Steps / 2][].Select(_ => new int[SizeElement])
                .ToArray()
        };

        result.Points.Add(parameters.Center);

        for (int i = 0; i < parameters.Steps; i++)
        {
            double newX = parameters.Radius * Math.Cos(parameters.Angle * i);
            double newY = parameters.Radius * Math.Sin(parameters.Angle * i);

            result.Points.Add(new(newX, newY));
        }

        if (result.Elements.Length == 1)
        {
            result.Elements[0][0] = 0;
            result.Elements[0][1] = 1;
            result.Elements[0][2] = 2;
            result.Elements[0][3] = 3;

            return (result.Points.Skip(1).ToList(), result.Elements);
        }

        var idx = 0;

        for (int i = 0, k = 0; i < parameters.Steps / 2; i++, k += 2)
        {
            result.Elements[idx][0] = 0;
            result.Elements[idx][1] = k + 1;
            result.Elements[idx][2] = k + 2;
            result.Elements[idx++][3] = k + 3;
        }

        result.Elements[idx - 1][3] = 1; // last node is starting + 1
        result.Elements[idx - 1] = result.Elements[idx - 1].OrderBy(v => v).ToArray(); // ordering

        using StreamWriter sw = new("output/elements.txt"), sw1 = new("output/points.txt");

        foreach (var elem in result.Elements)
        {
            foreach (var node in elem)
            {
                sw.Write(node + " ");
            }

            sw.WriteLine();
        }

        foreach (var point in result.Points)
        {
            sw1.WriteLine($"{point.X} {point.Y}");
        }

        result.Elements[0][0] = 0;
        result.Elements[0][1] = 1;
        result.Elements[0][2] = 2;
        result.Elements[0][3] = 3;

        // result.Elements[1][0] = 1;
        // result.Elements[1][1] = 2;
        // result.Elements[1][2] = 4;
        // result.Elements[1][3] = 5;
        //
        // result.Elements[2][0] = 3;
        // result.Elements[2][1] = 4;
        // result.Elements[2][2] = 6;
        // result.Elements[2][3] = 7;
        //
        // result.Elements[3][0] = 4;
        // result.Elements[3][1] = 5;
        // result.Elements[3][2] = 7;
        // result.Elements[3][3] = 8;

        var copy = result.Points.ToList();

        // result.Points[0] = copy[6];
        // result.Points[1] = copy[7];
        // result.Points[2] = copy[8];
        // result.Points[3] = copy[5];
        // result.Points[4] = copy[0];
        // result.Points[5] = copy[1];
        // result.Points[6] = copy[4];
        // result.Points[7] = copy[3];
        // result.Points[8] = copy[2];

        result.Points[0] = new(1, 1);
        result.Points[1] = new(5, 3);
        result.Points[2] = new(2, 5);
        result.Points[3] = new(4, 5);

        return (result.Points.SkipLast(5).ToList(), result.Elements.SkipLast(3).ToArray());
    }
}

public class CurveQuadraticMeshBuilder : MeshBuilder
{
    protected override int SizeElement => 9;

    public override (List<Point2D>, int[][]) Build(IParameters meshParameters)
    {
        if (meshParameters is not CurveMeshParameters parameters)
        {
            throw new ArgumentNullException(nameof(parameters), "Parameters mesh is null!");
        }

        var result = new
        {
            Points = new List<Point2D>(),
            Elements = new int[parameters.Steps / 2][].Select(_ => new int[SizeElement])
                .ToArray(),
        };

        return (result.Points, result.Elements);
    }
}

public interface IParameters
{
    public static abstract IParameters ReadJson(string jsonPath);
}

public readonly record struct MeshParameters
    (Interval IntervalX, int SplitsX, Interval IntervalY, int SplitsY) : IParameters
{
    public static IParameters ReadJson(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new("File does not exist");
        }

        using var sr = new StreamReader(jsonPath);
        return JsonConvert.DeserializeObject<MeshParameters>(sr.ReadToEnd());
    }
}

public readonly record struct CurveMeshParameters : IParameters
{
    [JsonIgnore] public double Angle { get; }
    public Point2D Center { get; }
    public double Radius { get; }
    public int Steps { get; }

    [JsonConstructor]
    public CurveMeshParameters(Point2D center, double radius, int steps)
    {
        Center = center;
        Radius = radius;
        Steps = steps;
        Angle = 2.0 * Math.PI / Steps;
    }

    public static IParameters ReadJson(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new("File does not exist");
        }

        using var sr = new StreamReader(jsonPath);
        return JsonConvert.DeserializeObject<CurveMeshParameters>(sr.ReadToEnd());
    }
}

public interface IBaseMesh
{
    IReadOnlyList<Point2D> Points { get; }
    IReadOnlyList<IReadOnlyList<int>> Elements { get; }
}

public class RegularMesh : IBaseMesh
{
    private readonly List<Point2D> _points;
    private readonly int[][] _elements;

    public IReadOnlyList<Point2D> Points => _points;
    public IReadOnlyList<IReadOnlyList<int>> Elements => _elements;

    public RegularMesh(IParameters meshParameters, MeshBuilder meshBuilder)
        => (_points, _elements) = meshBuilder.Build(meshParameters);
}