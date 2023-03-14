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
    protected abstract int ElementSize { get; }

    public abstract (List<Point2D>, FiniteElement[]) Build(IParameters meshParameters);

    protected (List<Point2D>, FiniteElement[]) BaseBuild(IParameters meshParameters)
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
            Elements = new FiniteElement[parameters.SplitsX * parameters.SplitsY]
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
        var nodes = new int[ElementSize];

        for (int j = 0; j < parameters.SplitsY; j++)
        {
            for (int i = 0; i < parameters.SplitsX; i++)
            {
                nodes[0] = i + j * nx;
                nodes[1] = i + 1 + j * nx;
                nodes[2] = i + (j + 1) * nx;
                nodes[3] = i + 1 + (j + 1) * nx;

                result.Elements[index++] = new(nodes.ToArray(), 0, 1.0);
            }
        }

        return (result.Points, result.Elements);
    }

    protected static void WriteToFile(List<Point2D> points, FiniteElement[] elements)
    {
        using StreamWriter sw = new("output/points.txt"), sw1 = new("output/elements.txt");

        foreach (var point in points)
        {
            sw.WriteLine($"{point.X} {point.Y}");
        }

        foreach (var element in elements)
        {
            foreach (var node in element.Nodes)
            {
                sw1.Write(node + " ");
            }

            sw1.Write(element.AreaNumber);
            sw1.WriteLine();
        }
    }
}

public class LinearMeshBuilder : MeshBuilder
{
    protected override int ElementSize => 4;

    public override (List<Point2D>, FiniteElement[]) Build(IParameters meshParameters)
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
    protected override int ElementSize => 9;

    public override (List<Point2D>, FiniteElement[]) Build(IParameters meshParameters)
    {
        if (meshParameters is not MeshParameters parameters)
        {
            throw new ArgumentNullException(nameof(parameters), "Parameters mesh is null!");
        }

        (List<Point2D> Points, FiniteElement[] Elements) result = BaseBuild(meshParameters);
        var pointsX = new double[2 * parameters.SplitsX + 1];
        var pointsY = new double[2 * parameters.SplitsY + 1];
        var vertices = new Point2D[9];

        pointsX.Fill(int.MinValue);
        pointsY.Fill(int.MinValue);

        foreach (var ielem in result.Elements)
        {
            var v1 = result.Points[ielem.Nodes[0]];
            var v2 = result.Points[ielem.Nodes[1]];
            var v3 = result.Points[ielem.Nodes[2]];
            var v4 = result.Points[ielem.Nodes[3]];

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
        var nodes = new int[ElementSize];

        for (int j = 0; j < parameters.SplitsY; j++)
        {
            for (int i = 0; i < parameters.SplitsX; i++)
            {
                nodes[0] = i + j * 2 * nx + i;
                nodes[1] = i + 1 + 2 * j * nx + i;
                nodes[2] = i + 2 + 2 * j * nx + i;
                nodes[3] = i + nx + 2 * j * nx + i;
                nodes[4] = i + nx + 1 + 2 * j * nx + i;
                nodes[5] = i + nx + 2 + 2 * j * nx + i;
                nodes[6] = i + 2 * nx + 2 * j * nx + i;
                nodes[7] = i + 2 * nx + 1 + 2 * j * nx + i;
                nodes[8] = i + 2 * nx + 2 + 2 * j * nx + i;

                result.Elements[index++] = new(nodes.ToArray(), 0, 1.0);
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
    protected override int ElementSize => 4;

    public override (List<Point2D>, FiniteElement[]) Build(IParameters meshParameters)
    {
        if (meshParameters is not CurveMeshParameters parameters)
        {
            throw new ArgumentNullException(nameof(parameters), "Parameters mesh is null!");
        }

        var radiiList = new List<double> { parameters.Radius2, parameters.Radius1 };

        int count;
        for (int k = 0; k < parameters.Splits; k++)
        {
            count = radiiList.Count;

            for (int i = 0; i < count - 1; i++)
            {
                radiiList.Add((radiiList[i] + radiiList[i + 1]) / 2.0);
            }

            radiiList = radiiList.OrderByDescending(v => v).ToList();
        }

        var result = new
        {
            Points = new List<Point2D>(),
            Elements = new FiniteElement[parameters.Steps * (radiiList.Count - 1)]
        };

        foreach (var radius in radiiList)
        {
            for (int i = 0; i < parameters.Steps; i++)
            {
                double newX = radius * Math.Cos(parameters.Angle * i) + parameters.Center.X;
                double newY = radius * Math.Sin(parameters.Angle * i) + parameters.Center.Y;

                result.Points.Add(new(newX, newY));
            }
        }

        parameters.RadiiCounts = radiiList.Count; // TODO

        var idx = 0;
        var pass = false;
        var step = 0;
        count = 0;
        var nodes = new int[ElementSize];
        var numberArea = 0;

        for (int i = 0; i < (radiiList.Count - 1) * parameters.Steps; i++)
        {
            if (!pass)
            {
                nodes[0] = i;
                nodes[1] = i + 1;
                nodes[2] = nodes[0] + parameters.Steps;
                nodes[3] = nodes[1] + parameters.Steps;
                step++;

                result.Elements[idx++] =
                    new(nodes.ToArray(), numberArea, parameters.Coeffs[numberArea]);

                if (step != parameters.Steps - 1) continue;
                pass = true;
                step = 0;
            }
            else
            {
                nodes[0] = count * parameters.Steps;
                nodes[1] = result.Elements[idx - 1].Nodes[1];
                nodes[2] = result.Elements[idx - 1].Nodes[1] + 1;
                count++;
                nodes[3] = (count + 1) * parameters.Steps - 1;
                pass = false;

                var checkPoint = result.Points[nodes[2]];
                var mediumRadius =
                    (parameters.Center.X + parameters.Radius1 + parameters.Center.X + parameters.Radius2) / 2.0;

                result.Elements[idx++] =
                    new(nodes.ToArray(), numberArea, parameters.Coeffs[numberArea]);

                if (checkPoint.X <= mediumRadius && checkPoint.X >= parameters.Center.X + parameters.Radius1)
                {
                    numberArea = 1;
                }
            }
        }

        WriteToFile(result.Points, result.Elements);

        return (result.Points, result.Elements.ToArray());
    }
}

public class CurveQuadraticMeshBuilder : MeshBuilder
{
    protected override int ElementSize => 9;

    public override (List<Point2D>, FiniteElement[]) Build(IParameters meshParameters)
    {
        if (meshParameters is not CurveMeshParameters parameters)
        {
            throw new ArgumentNullException(nameof(parameters), "Parameters mesh is null!");
        }

        var radiiList = new List<double>
            { parameters.Radius2, (parameters.Radius1 + parameters.Radius2) / 2.0, parameters.Radius1 };

        int count;

        for (int k = 0; k < parameters.Splits; k++)
        {
            count = radiiList.Count;

            for (int i = 0; i < count - 1; i++)
            {
                radiiList.Add((radiiList[i] + radiiList[i + 1]) / 2.0);
            }

            radiiList = radiiList.OrderByDescending(v => v).ToList();
        }

        var result = new
        {
            Points = new List<Point2D>(),
            Elements = new FiniteElement[parameters.Steps * (radiiList.Count / 2)]
        };

        int newSteps = parameters.Steps * 2;
        double newAngle = 2.0 * Math.PI / newSteps;

        foreach (var radius in radiiList)
        {
            // var extraDistance = RecalculateExtraDistance(radius);
            // var extraDistance = 0.0;

            for (int i = 0; i < newSteps; i++)
            {
                // double value = 0.0;

                // if (i % 2 != 0) value = extraDistance;

                double newX = radius * Math.Cos(newAngle * i) + parameters.Center.X;
                double newY = radius * Math.Sin(newAngle * i) + parameters.Center.Y;

                result.Points.Add(new(newX, newY));
            }
        }

        parameters.RadiiCounts = radiiList.Count; // TODO

        var idx = 0;
        var pass = false;
        var step = 0;
        count = 0;
        var nodes = new int[ElementSize];
        var numberArea = 0;

        for (int i = 0, k = 0; i < parameters.Steps * (radiiList.Count / 2); i++, k += 2)
        {
            if (!pass)
            {
                nodes[0] = k;
                nodes[1] = k + 1;
                nodes[2] = k + 2;
                nodes[3] = nodes[0] + newSteps;
                nodes[4] = nodes[1] + newSteps;
                nodes[5] = nodes[2] + newSteps;
                nodes[6] = nodes[0] + 2 * newSteps;
                nodes[7] = nodes[1] + 2 * newSteps;
                nodes[8] = nodes[2] + 2 * newSteps;
                step++;

                result.Elements[idx++] = new(nodes.ToArray(), numberArea, parameters.Coeffs[numberArea]);

                if (step != parameters.Steps - 1) continue;
                pass = true;
                step = 0;
            }
            else
            {
                nodes[0] = result.Elements[idx - 1].Nodes[2];
                nodes[1] = nodes[0] + 1;
                nodes[2] = count * newSteps;
                count += 2;
                nodes[3] = result.Elements[idx - 1].Nodes[5];
                nodes[4] = result.Elements[idx - 1].Nodes[5] + 1;
                nodes[5] = nodes[2] + newSteps;
                nodes[6] = result.Elements[idx - 1].Nodes[8];
                nodes[7] = result.Elements[idx - 1].Nodes[8] + 1;
                nodes[8] = nodes[5] + newSteps;
                k = nodes[8] - 2;
                pass = false;

                var checkPoint = result.Points[nodes[8]];
                var mediumRadius =
                    (parameters.Center.X + parameters.Radius1 + parameters.Center.X + parameters.Radius2) / 2.0;

                result.Elements[idx++] =
                    new(nodes.ToArray(), numberArea, parameters.Coeffs[numberArea]);

                if (checkPoint.X <= mediumRadius && checkPoint.X >= parameters.Center.X + parameters.Radius1)
                {
                    numberArea = 1;
                }
            }
        }

        WriteToFile(result.Points, result.Elements);

        return (result.Points, result.Elements.ToArray());

        double RecalculateExtraDistance(double radius)
        {
            var mediumPoint = ((radius * Math.Cos(0.0) + parameters.Center.X,
                                   radius * Math.Sin(0.0) + parameters.Center.Y) +
                               new Point2D(radius * Math.Cos(2 * newAngle) + parameters.Center.X,
                                   radius * Math.Sin(2 * newAngle) + parameters.Center.Y)) / 2.0;

            var extraPoint = (radius * Math.Cos(newAngle) + parameters.Center.X,
                radius * Math.Sin(newAngle) + parameters.Center.Y);

            return Math.Sqrt((extraPoint - mediumPoint) * (extraPoint - mediumPoint));
        }
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

public class CurveMeshParameters : IParameters
{
    [JsonIgnore] public double Angle { get; }
    public Point2D Center { get; }
    public double Radius1 { get; }
    public double Radius2 { get; }
    public int Steps { get; }
    public int Splits { get; }
    public double[] Coeffs { get; }
    public int? RadiiCounts { get; set; }

    [JsonConstructor]
    public CurveMeshParameters(Point2D center, double radius1, double radius2, int steps, int splits, double[] coeffs)
    {
        Center = center;
        Radius1 = radius1;
        Radius2 = radius2;
        Steps = steps;
        Angle = 2.0 * Math.PI / Steps;
        Splits = splits;
        Coeffs = coeffs;

        if (radius1 <= 0 || radius2 <= 0 || Math.Abs(radius1 - radius2) < 1E-07)
        {
            throw new ArgumentException("Incorrect data in mesh parameters");
        }
    }

    public static IParameters ReadJson(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new("File does not exist");
        }

        using var sr = new StreamReader(jsonPath);
        return JsonConvert.DeserializeObject<CurveMeshParameters>(sr.ReadToEnd()) ??
               throw new NullReferenceException("Incorrect mesh parameters!");
    }
}

public interface IBaseMesh
{
    IReadOnlyList<Point2D> Points { get; }
    IReadOnlyList<FiniteElement> Elements { get; }
}

public class RegularMesh : IBaseMesh
{
    private readonly List<Point2D> _points;
    private readonly FiniteElement[] _elements;

    public IReadOnlyList<Point2D> Points => _points;
    public IReadOnlyList<FiniteElement> Elements => _elements;

    public RegularMesh(IParameters meshParameters, MeshBuilder meshBuilder)
        => (_points, _elements) = meshBuilder.Build(meshParameters);
}