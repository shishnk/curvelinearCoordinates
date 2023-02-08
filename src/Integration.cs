namespace Project;

public class Integration
{
    private readonly IEnumerable<QuadratureNode<double>> _quadratures;

    public Integration(IEnumerable<QuadratureNode<double>> quadratures) => _quadratures = quadratures;

    public double Gauss2D(Func<Point2D, double> psi, Rectangle element)
    {
        double hx = element.RightTop.X - element.LeftTop.X;
        double hy = element.RightTop.Y - element.RightBottom.Y;
        
        var result = (from qi in _quadratures
            from qj in _quadratures
            let point = new Point2D((qi.Node * hx + element.LeftBottom.X + element.RightBottom.X) / 2.0,
                qj.Node * hy + element.LeftTop.Y + element.RightTop.Y / 2.0)
            select psi(point) * qi.Weight * qj.Weight).Sum();

        return result * hx * hy / 4.0;
    }
}