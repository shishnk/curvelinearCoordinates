namespace Project.Tests;

public interface ITest
{
    double U(Point2D point);

    double F(Point2D point);
}

public class Test1 : ITest
{
    public double U(Point2D point) => point.X + point.Y;

    public double F(Point2D point) => 0.0;
}

public class Test2 : ITest
{
    public double U(Point2D point) => point.X * point.X + point.Y * point.Y;

    public double F(Point2D point) => -4.0;
}

public class Test3 : ITest
{
    public double U(Point2D point) => point.X * point.X * point.X * point.Y * point.Y * point.Y;

    public double F(Point2D point) =>
        -6.0 * point.X * point.Y * point.Y * point.Y - 6.0 * point.Y * point.X * point.X * point.X;
}

public class Test4 : ITest
{
    public double U(Point2D point) => point.X * point.X * point.X * point.X + point.Y * point.Y * point.Y * point.Y;

    public double F(Point2D point) => -12.0 * point.X * point.X - 12.0 * point.Y * point.Y;
}

public class Test5 : ITest
{
    public double U(Point2D point) => Math.Sin(point.X);

    public double F(Point2D point) => Math.Sin(point.X);
}

public class Test6 : ITest
{
    public double U(Point2D point) => Math.Exp(point.X + point.Y);

    public double F(Point2D point) => -2.0 * Math.Exp(point.X + point.Y);
}

public class Test7 : ITest
{
    public double U(Point2D point) => (point.X + 1) * (point.X + 1) * (point.X + 1) * point.Y * point.Y * point.Y;

    public double F(Point2D point) => -6.0 * (point.X + 1) * point.Y * point.Y * point.Y -
                                      6.0 * point.Y * (point.X + 1) * (point.X + 1) * (point.X + 1);
}

// public class Test8 : ITest
// {
//     public double U(Point2D point, int areaNumber) =>
//         areaNumber == 0
//             ? 10.0 / 17.0 * (point.X * point.X + point.Y * point.Y) * (point.X * point.X + point.Y * point.Y) -
//               140.0 / 17.0
//             : 20.0 / 17.0 * (point.X * point.X + point.Y * point.Y) * (point.X * point.X + point.Y * point.Y);
//
//     public double F(Point2D point) =>
//         -80.0 / 17.0 * (point.X * point.X + point.Y * point.Y) - 160.0 / 17.0 * point.X * point.X -
//         80.0 / 17.0 * (point.X * point.X + point.Y * point.Y) - 160.0 / 17.0 * point.Y * point.Y;
// }