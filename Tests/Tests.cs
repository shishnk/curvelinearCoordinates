namespace Project.Tests;

public interface ITest
{
    double U(Point2D point);

    double F(Point2D point);
}

public class Test1 : ITest
{
    public double U(Point2D point) => point.X * point.Y;

    public double F(Point2D point) => -2.0 + point.X * point.Y;
}