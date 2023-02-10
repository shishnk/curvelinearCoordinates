namespace Project.Meshes;

public interface IMeshCreator
{
    IBaseMesh CreateMesh(MeshParameters meshParameters, MeshBuilder? meshBuilder = null);
}

public class RegularMeshCreator : IMeshCreator
{
    public IBaseMesh CreateMesh(MeshParameters meshParameters, MeshBuilder? meshBuilder = null) =>
        new RegularMesh(meshParameters, meshBuilder ?? new MeshLinearBuilder());
}

// public class IrregularMesh : BaseMesh TODO may be

public abstract class MeshBuilder
{
    public abstract (List<Point2D>, int[][]) Build(MeshParameters meshParameters);

    protected static (List<Point2D>, int[][]) BaseBuild(MeshParameters meshParameters, int sizeElement)
    {
        var result = new
        {
            Points = new List<Point2D>(),
            Elements = new int[meshParameters.SplitsX * meshParameters.SplitsY][].Select(_ => new int[sizeElement])
                .ToArray(),
        };

        double hx = meshParameters.IntervalX.Length / meshParameters.SplitsX;
        double hy = meshParameters.IntervalY.Length / meshParameters.SplitsY;

        double[] pointsX = new double[meshParameters.SplitsX + 1];
        double[] pointsY = new double[meshParameters.SplitsY + 1];

        pointsX[0] = meshParameters.IntervalX.LeftBorder;
        pointsY[0] = meshParameters.IntervalY.LeftBorder;

        for (int i = 1; i < meshParameters.SplitsX + 1; i++)
        {
            pointsX[i] = pointsX[i - 1] + hx;
        }

        for (int i = 1; i < meshParameters.SplitsY + 1; i++)
        {
            pointsY[i] = pointsY[i - 1] + hy;
        }

        for (int j = 0; j < meshParameters.SplitsY + 1; j++)
        {
            for (int i = 0; i < meshParameters.SplitsX + 1; i++)
            {
                result.Points.Add(new(pointsX[i], pointsY[j]));
            }
        }

        int nx = meshParameters.SplitsX + 1;
        int index = 0;

        for (int j = 0; j < meshParameters.SplitsY; j++)
        {
            for (int i = 0; i < meshParameters.SplitsX; i++)
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

public class MeshLinearBuilder : MeshBuilder
{
    public override (List<Point2D>, int[][]) Build(MeshParameters meshParameters)
    {
        var result = BaseBuild(meshParameters, 4);

        return (result.Item1, result.Item2);
    }
}

public class MeshQuadraticBuilder : MeshBuilder
{
    public override (List<Point2D>, int[][]) Build(MeshParameters meshParameters)
    {
        (List<Point2D> Points, int[][] Elements) result = BaseBuild(meshParameters, 9);
        var pointsX = new double[2 * meshParameters.SplitsX + 1];
        var pointsY = new double[2 * meshParameters.SplitsY + 1];
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

        var nx = 2 * meshParameters.SplitsX + 1;
        var index = 0;

        for (int j = 0; j < meshParameters.SplitsY; j++)
        {
            for (int i = 0; i < meshParameters.SplitsX; i++)
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

public readonly record struct MeshParameters(Interval IntervalX, int SplitsX, Interval IntervalY, int SplitsY)
{
    public static MeshParameters ReadJson(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new("File does not exist");
        }

        using var sr = new StreamReader(jsonPath);
        return JsonConvert.DeserializeObject<MeshParameters>(sr.ReadToEnd());
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

    public RegularMesh(MeshParameters meshParameters, MeshBuilder meshBuilder)
    {
        if (meshParameters.SplitsX < 1 || meshParameters.SplitsY < 1)
        {
            throw new("The number of splits must be greater than or equal to 1");
        }

        (_points, _elements) = meshBuilder.Build(meshParameters);
    }
}