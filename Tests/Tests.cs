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
    public double U(Point2D point) => point.X * point.X + point.Y;

    public double F(Point2D point) => -2.0;
}

public class Test3 : ITest
{
    public double U(Point2D point) => point.X * point.X * point.X * point.Y * point.Y * point.Y;

    public double F(Point2D point) =>
        -6.0 * point.X * point.Y * point.Y * point.Y - 6.0 * point.Y * point.X * point.X * point.X + U(point);
}

public class Test4 : ITest
{
    public double U(Point2D point) => point.X * point.X * point.X * point.X + point.Y * point.Y * point.Y * point.Y;

    public double F(Point2D point) => -12.0 * point.X * point.X - 12.0 * point.Y * point.Y;
}