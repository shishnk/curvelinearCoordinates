namespace Project;

public class Integration
{
    private readonly IEnumerable<QuadratureNode<double>> _quadratures;

    public Integration(IEnumerable<QuadratureNode<double>> quadratures) => _quadratures = quadratures;

    public double GaussSegment(Func<Point2D, double> psi, Rectangle element)
    {
        double hx = element.RightTop.X - element.LeftTop.X;
        double hy = element.RightBottom.Y - element.LeftBottom.Y;
        double result = 0.0;

        foreach (var qi in _quadratures)
        {
            foreach (var qj in _quadratures)
            {
                var point = new Point2D((qi.Node * hx + element.LeftBottom.X + element.RightBottom.X) / 2.0,
                    qj.Node * hy + element.LeftTop.Y + element.RightTop.Y / 2.0);

                result += psi(point) * qi.Weight * qj.Weight;
            }
        }

        return result * hx * hy / 4.0;
    }
}