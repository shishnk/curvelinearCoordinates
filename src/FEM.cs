namespace Project;

public class SolverFem
{
    public class SolverFemBuilder
    {
        private readonly SolverFem _solverFem = new();

        public SolverFemBuilder SetTest(ITest test)
        {
            _solverFem._test = test;
            return this;
        }

        public SolverFemBuilder SetMesh(IBaseMesh mesh)
        {
            _solverFem._mesh = mesh;
            return this;
        }

        public SolverFemBuilder SetSolverSlae(IterativeSolver iterativeSolver)
        {
            _solverFem._iterativeSolver = iterativeSolver;
            return this;
        }

        public SolverFemBuilder SetBoundaries(IEnumerable<IBoundary> boundaries)
        {
            _solverFem._boundaries = boundaries.DistinctBy(b => b.Node);
            return this;
        }

        public SolverFemBuilder SetAssembler(BaseMatrixAssembler matrixAssembler)
        {
            _solverFem._matrixAssembler = matrixAssembler;
            return this;
        }

        public static implicit operator SolverFem(SolverFemBuilder builder)
            => builder._solverFem;
    }

    private IBaseMesh _mesh = default!;
    private ITest _test = default!;
    private IterativeSolver _iterativeSolver = default!;
    private IEnumerable<IBoundary> _boundaries = default!;
    private Vector<double> _localVector = default!;
    private Vector<double> _globalVector = default!;
    private BaseMatrixAssembler _matrixAssembler = default!;
    private Newton? _newton;

    public void Compute()
    {
        Initialize();
        AssemblySystem();
        // _matrixAssembler.GlobalMatrix!.PrintDense("output/matrixBefore.txt");
        AccountingDirichletBoundary();

        // _matrixAssembler.GlobalMatrix.PrintDense("output/matrixAfter.txt");

        _iterativeSolver.SetMatrix(_matrixAssembler.GlobalMatrix!);
        _iterativeSolver.SetVector(_globalVector);
        _iterativeSolver.Compute();

        var exact = new double[_mesh.Points.Count];

        for (int i = 0; i < exact.Length; i++)
        {
            exact[i] = _test.U(_mesh.Points[i]);
        }

        var result = exact.Zip(_iterativeSolver.Solution!.Value, (v1, v2) => (v2, v1));

        foreach (var (v1, v2) in result)
        {
            Console.WriteLine($"{v1} ------------ {v2} ");
        }

        Console.WriteLine("---------------------------");

        // var exact = (from element in _mesh.Elements
        //     from node in element.Nodes
        //     select (_test.U(_mesh.Points[node], element.AreaNumber), node)).ToList();
        //
        // exact = exact.DistinctBy(tuple => tuple.Item2).OrderBy(tuple => tuple.Item2).ToList();
        //
        // var approx = _iterativeSolver.Solution!.Value.ToList();
        //
        // var result = exact.Zip(approx, (v1, v2) => (v2, v1.Item1));
        //
        // foreach (var (v1, v2) in result)
        // {
        //     Console.WriteLine($"{v1} ------------ {v2} ");
        // }
        //
        // Console.WriteLine("---------------------------");

        CalculateError();
        // CalculateErrorWithBreaking(approx, exact.Select(tuple => tuple.Item1).ToList());
    }

    private void Initialize()
    {
        PortraitBuilder.Build(_mesh, out var ig, out var jg);
        _matrixAssembler.GlobalMatrix = new(ig.Length - 1, jg.Length)
        {
            Ig = ig,
            Jg = jg
        };

        _globalVector = new(ig.Length - 1);
        _localVector = new(_matrixAssembler.BasisSize);
    }

    private void AssemblySystem()
    {
        for (int ielem = 0; ielem < _mesh.Elements.Count; ielem++)
        {
            var element = _mesh.Elements[ielem];

            _matrixAssembler.BuildLocalMatrices(ielem);
            BuildLocalVector(ielem);

            for (int i = 0; i < _matrixAssembler.BasisSize; i++)
            {
                _globalVector[element.Nodes[i]] += _localVector[i];

                for (int j = 0; j < _matrixAssembler.BasisSize; j++)
                {
                    _matrixAssembler.FillGlobalMatrix(element.Nodes[i], element.Nodes[j],
                        _matrixAssembler.StiffnessMatrix[i, j]);
                }
            }
        }
    }

    private void BuildLocalVector(int ielem)
    {
        _localVector.Fill(0.0);

        for (int i = 0; i < _matrixAssembler.BasisSize; i++)
        {
            for (int j = 0; j < _matrixAssembler.BasisSize; j++)
            {
                _localVector[i] += _matrixAssembler.MassMatrix[i, j] *
                                   _test.F(_mesh.Points[_mesh.Elements[ielem].Nodes[j]]);
            }
        }
    }

    private void AccountingDirichletBoundary()
    {
        int[] checkBc = new int[_mesh.Points.Count];

        checkBc.Fill(-1);
        var boundariesArray = _boundaries.ToArray();

        for (int i = 0; i < boundariesArray.Length; i++)
        {
            checkBc[boundariesArray[i].Node] = i;
            boundariesArray[i].Value = _test.U(_mesh.Points[boundariesArray[i].Node]);
        }

        // for (int i = 0, k = boundariesArray.Length - 1;
        //      i < boundariesArray.Length / 2;
        //      i++, k--)
        // {
        //     boundariesArray[i].Value = 10.0;
        //     boundariesArray[k].Value = 0.0;
        // }

        for (int i = 0; i < _mesh.Points.Count; i++)
        {
            int index;
            if (checkBc[i] != -1)
            {
                _matrixAssembler.GlobalMatrix!.Di[i] = 1.0;
                _globalVector[i] = boundariesArray[checkBc[i]].Value;

                for (int k = _matrixAssembler.GlobalMatrix.Ig[i]; k < _matrixAssembler.GlobalMatrix.Ig[i + 1]; k++)
                {
                    index = _matrixAssembler.GlobalMatrix.Jg[k];

                    if (checkBc[index] == -1)
                    {
                        _globalVector[index] -= _matrixAssembler.GlobalMatrix.Gg[k] * _globalVector[i];
                    }

                    _matrixAssembler.GlobalMatrix.Gg[k] = 0.0;
                }
            }
            else
            {
                for (int k = _matrixAssembler.GlobalMatrix!.Ig[i]; k < _matrixAssembler.GlobalMatrix.Ig[i + 1]; k++)
                {
                    index = _matrixAssembler.GlobalMatrix.Jg[k];

                    if (checkBc[index] == -1) continue;
                    _globalVector[i] -= _matrixAssembler.GlobalMatrix.Gg[k] * _globalVector[index];
                    _matrixAssembler.GlobalMatrix.Gg[k] = 0.0;
                }
            }
        }
    }

    private void CalculateError()
    {
        var error = new double[_mesh.Points.Count];

        for (int i = 0; i < error.Length; i++)
        {
            error[i] = Math.Abs(_iterativeSolver.Solution!.Value[i] - _test.U(_mesh.Points[i]));
        }

        Array.ForEach(error, Console.WriteLine);

        var sum = error.Sum(t => t * t);

        sum = Math.Sqrt(sum / _mesh.Points.Count);

        Console.WriteLine($"rms = {sum}");

        using var sw = new StreamWriter("output/3.csv");

        for (int i = 0; i < error.Length; i++)
        {
            if (i == 0)
            {
                sw.WriteLine("$i$, $u_i^*$, $u_i$, $|u^* - u|$, Погрешность");
                sw.WriteLine(
                    $"{i}, {_test.U(_mesh.Points[i])}, {_iterativeSolver.Solution!.Value[i]}, {error[i]}, {sum}");
                continue;
            }

            sw.WriteLine($"{i}, {_test.U(_mesh.Points[i])}, {_iterativeSolver.Solution!.Value[i]}, {error[i]},");
        }
    }

    private void CalculateErrorWithBreaking(IReadOnlyList<double> approx, IReadOnlyList<double> exact)
    {
        var error = new double[approx.Count];

        for (int i = 0; i < error.Length; i++)
        {
            error[i] = Math.Abs(approx[i] - exact[i]);
        }

        Array.ForEach(error, Console.WriteLine);

        var sum = error.Sum(t => t * t);

        sum = Math.Sqrt(sum / _mesh.Points.Count);

        Console.WriteLine($"rms = {sum}");
    }

    public void DoResearch()
    {
        const int steps = 480;
        const double angle = 2 * Math.PI / steps;

        var pointsY = new List<double>();

        for (int i = 0; i < steps + 1; i++)
        {
            pointsY.Add(angle * i);
        }

        Span<double> pointsX = stackalloc double[2] { 1.5, 2.5 };

        var result = 0.0;

        for (int i = 1; i < pointsX.Length; i++)
        {
            // var hx = Math.Abs(pointsX[i] - pointsX[i - 1]);
            var x = (pointsX[i - 1] + pointsX[i]) / 2.0;
            var r = x;

            for (int j = 1; j < pointsY.Count; j++)
            {
                var hy = Math.Abs(pointsY[j] - pointsY[j - 1]);
                var y = (pointsY[j - 1] + pointsY[j]) / 2.0;

                var area = Math.PI * pointsX[i] * pointsX[i] * hy / (2.0 * Math.PI) -
                           Math.PI * pointsX[i - 1] * pointsX[i - 1] * hy / (2.0 * Math.PI);

                x = r * Math.Cos(y);
                y = r * Math.Sin(y);

                result += Math.Abs(_test.U((x, y)) - CalculateAtPoint((x, y), true)) * area;
                // result += area;
            }
        }

        Console.WriteLine(result);
    }

    public double CalculateAtPoint(Point2D point, bool printResult = false)
    {
        int ielem = FindNumberElement(point);

        var element = _mesh.Elements[ielem];
        var result = 0.0;

        _newton ??= new(_matrixAssembler.Basis, _mesh);

        _newton.Point = point;
        _newton.NumberElement = ielem;

        _newton.Compute();

        for (int i = 0; i < _matrixAssembler.BasisSize; i++)
        {
            result += _iterativeSolver.Solution!.Value[element.Nodes[i]] *
                      _matrixAssembler.Basis.GetPsi(i, _newton.Result);
        }

        if (printResult) Console.WriteLine($"Value at {point} = {result}");
        return result;
    }

    private double CalculateAtPoint(Point2D point, FiniteElement element)
    {
        var result = 0.0;

        for (int i = 0; i < _matrixAssembler.BasisSize; i++)
        {
            result += _iterativeSolver.Solution!.Value[element.Nodes[i]] *
                      _matrixAssembler.Basis.GetPsi(i, point);
        }

        return result;
    }

    private int FindNumberElement(Point2D point)
    {
        const double floatEps = 1E-07;
        const double eps = 1E-05;

        for (int ielem = 0; ielem < _mesh.Elements.Count; ielem++)
        {
            var element = _mesh.Elements[ielem];

            var intersectionCounter = 0;

            var edges = new List<(Point2D, Point2D)>
            {
                (_mesh.Points[element.Nodes[0]], _mesh.Points[element.Nodes[1]]),
                (_mesh.Points[element.Nodes[1]], _mesh.Points[element.Nodes[2]]),
                (_mesh.Points[element.Nodes[2]], _mesh.Points[element.Nodes[8]]),
                (_mesh.Points[element.Nodes[8]], _mesh.Points[element.Nodes[7]]),
                (_mesh.Points[element.Nodes[7]], _mesh.Points[element.Nodes[6]]),
                (_mesh.Points[element.Nodes[6]], _mesh.Points[element.Nodes[0]])
            };

            foreach (var edge in edges)
            {
                if (OnEdge(edge)) return ielem;

                var pt = point;
                (Point2D A, Point2D B) sortedEdge = edge.Item1.Y > edge.Item2.Y ? (edge.Item2, edge.Item1) : edge;

                if (Math.Abs(pt.Y - sortedEdge.A.Y) < floatEps || Math.Abs(pt.Y - sortedEdge.B.Y) < floatEps)
                {
                    pt = pt with { Y = pt.Y + eps };
                }

                var maxX = edge.Item1.X > edge.Item2.X ? edge.Item1.X : edge.Item2.X;
                var minX = edge.Item1.X < edge.Item2.X ? edge.Item1.X : edge.Item2.X;

                if (pt.Y > sortedEdge.B.Y || pt.Y < sortedEdge.A.Y || pt.X > maxX) continue;

                if (pt.X < minX)
                {
                    intersectionCounter++;
                }
                else
                {
                    var mRed = Math.Abs(sortedEdge.A.X - sortedEdge.B.X) > uint.MinValue
                        ? (sortedEdge.B.Y - sortedEdge.A.Y) / (sortedEdge.B.X - sortedEdge.A.X)
                        : double.PositiveInfinity;
                    var mBlue = Math.Abs(sortedEdge.A.X - pt.X) > uint.MinValue
                        ? (pt.Y - sortedEdge.A.Y) / (pt.X - sortedEdge.A.X)
                        : double.PositiveInfinity;

                    if (mBlue >= mRed) intersectionCounter++;
                }
            }

            if (intersectionCounter % 2 == 1) return ielem;

            bool OnEdge((Point2D, Point2D) edge)
            {
                var distance1 = Math.Sqrt((point - edge.Item1) * (point - edge.Item1));
                var distance2 = Math.Sqrt((point - edge.Item2) * (point - edge.Item2));
                var edgeLenght = Math.Sqrt((edge.Item2 - edge.Item1) * (edge.Item2 - edge.Item1));

                return Math.Abs(distance1 + distance2 - edgeLenght) < floatEps;
            }
        }

        throw new("Not support exception!");
    }

    // private int FindNumberElement(Point2D point)
    // {
    //     var pt = new Point2D(Math.Sqrt(point.X * point.X + point.Y * point.Y), CalculateAtan(point.Y, point.X));
    //
    //     for (var ielem = 0; ielem < _mesh.Elements.Count; ielem++)
    //     {
    //         var element = _mesh.Elements[ielem];
    //
    //         Point2D vert1 = (Math.Sqrt(_mesh.Points[element.Nodes[0]].X * _mesh.Points[element.Nodes[0]].X +
    //                                    _mesh.Points[element.Nodes[0]].Y * _mesh.Points[element.Nodes[0]].Y),
    //             CalculateAtan(_mesh.Points[element.Nodes[0]].Y, _mesh.Points[element.Nodes[0]].X));
    //         Point2D vert4 = (Math.Sqrt(_mesh.Points[element.Nodes[8]].X * _mesh.Points[element.Nodes[8]].X +
    //                                    _mesh.Points[element.Nodes[8]].Y * _mesh.Points[element.Nodes[8]].Y),
    //             CalculateAtan(_mesh.Points[element.Nodes[8]].Y, _mesh.Points[element.Nodes[8]].X));
    //
    //         if (pt.X >= vert4.X && pt.X <= vert1.X && pt.Y >= vert1.Y && pt.Y <= vert4.Y) return ielem;
    //     }
    //
    //     double CalculateAtan(double y, double x)
    //     {
    //         
    //         
    //         var atan = Math.Atan2(y, x);
    //         return atan % (2.0 * Math.PI);
    //     }
    //
    //     throw new("Not support exception!");
    // }

    // private double CalculateElementArea(int ielem)
    // {
    // var element = _mesh.Elements[ielem];
    //
    // var side1 = Math.Sqrt((_mesh.Points[element.Nodes[8]] - _mesh.Points[element.Nodes[2]]) *
    //                       (_mesh.Points[element.Nodes[8]] - _mesh.Points[element.Nodes[2]]));
    // var side2 = Math.Sqrt((_mesh.Points[element.Nodes[6]] - _mesh.Points[element.Nodes[2]]) *
    //                       (_mesh.Points[element.Nodes[6]] - _mesh.Points[element.Nodes[2]]));
    // var side3 = Math.Sqrt((_mesh.Points[element.Nodes[8]] - _mesh.Points[element.Nodes[6]]) *
    //                       (_mesh.Points[element.Nodes[8]] - _mesh.Points[element.Nodes[6]]));
    //
    // var semiperimeter = 1.0 / 2.0 * (side1 + side2 + side3);
    // var triangleArea1 = Math.Sqrt(semiperimeter * (semiperimeter - side1) * (semiperimeter - side2) *
    //                               (semiperimeter - side3));
    //
    // side1 = Math.Sqrt((_mesh.Points[element.Nodes[2]] - _mesh.Points[element.Nodes[0]]) *
    //                   (_mesh.Points[element.Nodes[2]] - _mesh.Points[element.Nodes[0]]));
    // side3 = Math.Sqrt((_mesh.Points[element.Nodes[6]] - _mesh.Points[element.Nodes[0]]) *
    //                   (_mesh.Points[element.Nodes[6]] - _mesh.Points[element.Nodes[0]]));
    //
    // semiperimeter = 1.0 / 2.0 * (side1 + side2 + side3);
    // var triangleArea2 = Math.Sqrt(semiperimeter * (semiperimeter - side1) * (semiperimeter - side2) *
    //                               (semiperimeter - side3));
    //
    // return triangleArea1 + triangleArea2;

    //     var element = _mesh.Elements[ielem];
    //
    //     var vert1 = _mesh.Points[element.Nodes[0]];
    //     var vert2 = _mesh.Points[element.Nodes[1]];
    //     var vert3 = _mesh.Points[element.Nodes[2]];
    //     var vert4 = _mesh.Points[element.Nodes[8]];
    //     var vert5 = _mesh.Points[element.Nodes[7]];
    //     var vert6 = _mesh.Points[element.Nodes[6]];
    //
    //     return 1.0 / 2.0 * Math.Abs(vert1.X * vert2.Y + vert2.X * vert3.Y + vert3.X * vert4.Y +
    //                                 vert4.X * vert5.Y + vert5.X * vert6.Y + vert6.X * vert1.Y -
    //                                 vert2.X * vert1.Y - vert3.X * vert2.Y - vert4.X * vert3.Y -
    //                                 vert5.X * vert4.Y - vert6.X * vert5.Y - vert1.X * vert6.Y);
    // }

    public static SolverFem.SolverFemBuilder CreateBuilder() => new();

    public double Integrate()
    {
        var result = 0.0;

        foreach (var element in _mesh.Elements)
        {
            result += IntegrateElement(element);
        }

        Console.WriteLine(result);
        return result;
    }

    private double IntegrateElement(FiniteElement element)
    {
        var k = 1;
        var quadratures = Quadratures.SegmentGaussOrder5().Select(node => ((node.Node + 1.0) / 2.0, node.Weight / 2.0));
        var lastResult = 0.0;
        double result;

        Span<double> dx = stackalloc double[2];
        Span<double> dy = stackalloc double[2];

        while (true)
        {
            var x = 0.0;
            var h = 1.0 / k;
            result = 0.0;

            for (int i = 0; i < k; i++, x += h)
            {
                var y = 0.0;

                for (int j = 0; j < k; j++, y += h)
                {
                    foreach (var qi in quadratures)
                    {
                        foreach (var qj in quadratures)
                        {
                            var point = (x + h * qi.Item1, y + h * qj.Item1);
                            var x1 = 0.0;
                            var y1 = 0.0;

                            dx[0] = 0.0;
                            dx[1] = 0.0;
                            dy[0] = 0.0;
                            dy[1] = 0.0;

                            for (int c = 0; c < _matrixAssembler.BasisSize; c++)
                            {
                                x1 += _matrixAssembler.Basis.GetPsi(c, point) * _mesh.Points[element.Nodes[c]].X;
                                y1 += _matrixAssembler.Basis.GetPsi(c, point) * _mesh.Points[element.Nodes[c]].Y;
                            }

                            for (int p = 0; p < _matrixAssembler.BasisSize; p++)
                            {
                                for (int r = 0; r < 2; r++)
                                {
                                    dx[r] += _matrixAssembler.Basis.GetDPsi(p, r, point) *
                                             _mesh.Points[element.Nodes[p]].X;
                                    dy[r] += _matrixAssembler.Basis.GetDPsi(p, r, point) *
                                             _mesh.Points[element.Nodes[p]].Y;
                                }
                            }

                            var determinant = dx[0] * dy[1] - dx[1] * dy[0];

                            var pt = (x1, y1);

                            result += Math.Abs(CalculateAtPoint(point, element) - _test.U(pt)) *
                                      Math.Abs(determinant) * qi.Item2 *
                                      qj.Item2 * h * h;
                        }
                    }
                }
            }

            if (k != 1)
            {
                if (Math.Abs((result - lastResult) / result) < 1E-10)
                {
                    break;
                }
            }

            k *= 2;

            lastResult = result;

            if (k == 32)
            {
                break;
            }
        }

        return result;
    }
}