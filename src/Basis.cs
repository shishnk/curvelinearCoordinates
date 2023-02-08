namespace Project;

public interface IBasis
{
    int Size { get; }

    public double GetPsi(int number, Point2D point);

    public double GetDPsi(int number, int varNumber, Point2D point);
}

public readonly record struct QuadraticBasis : IBasis
{
    public int Size => 9;

    public double GetPsi(int number, Point2D point)
        => number switch
        {
            0 => 4.0 * (point.X - 0.5) * (point.X - 1.0) * (point.Y - 0.5) * (point.Y - 1.0),
            1 => -8.0 * point.X * (point.X - 1.0) * (point.Y - 0.5) * (point.Y - 1.0),
            2 => 4.0 * point.X * (point.X - 0.5) * (point.Y - 0.5) * (point.Y - 1.0),
            3 => -8.0 * (point.X - 0.5) * (point.X - 1.0) * point.Y * (point.Y - 1.0),
            4 => -16.0 * point.X * point.Y * (point.X - 1.0) * (point.Y - 1.0),
            5 => -8.0 * point.X * point.Y * (point.X - 0.5) * (point.Y - 1.0),
            6 => 4.0 * point.Y * (point.X - 0.5) * (point.X - 1.0) * (point.Y - 0.5),
            7 => -8.0 * point.X * point.Y * (point.X - 1.0) * (point.Y - 0.5),
            8 => 4.0 * point.X * point.Y * (point.X - 0.5) * (point.Y - 0.5),
            _ => throw new ArgumentOutOfRangeException(nameof(number), number, "Not expected function number!")
        };

    public double GetDPsi(int number, int varNumber, Point2D point)
        => varNumber switch
        {
            1 => number switch
            {
                0 => 4.0 * (point.X - 1.0 + (point.X - 0.5)) * (point.Y - 0.5) * (point.Y - 0.5),
                1 => -8.0 * (point.X - 1.0 + point.X) * (point.Y - 0.5) * (point.Y - 1.0),
                2 => 4.0 * (point.X - 0.5 + point.X) * (point.Y - 0.5) * (point.Y - 1.0),
                3 => -8.0 * (point.X - 1.0 + (point.X - 0.5)) * point.Y * (point.Y - 1.0),
                4 => 16.0 * (point.X - 1.0 + point.X) * point.Y * (point.Y - 1.0),
                5 => -8.0 * (point.X - 0.5 + point.X) * point.Y * (point.Y - 1.0),
                6 => 4.0 * (point.X - 1.0 + (point.X - 0.5)) * point.Y * (point.Y - 0.5),
                7 => -8.0 * (point.X - 1.0 + point.X) * point.Y * (point.Y - 0.5),
                8 => 4.0 * (point.X - 0.5 + point.X) * point.Y * (point.Y - 0.5),
                _ => throw new ArgumentOutOfRangeException(nameof(number), number, "Not expected function number!")
            },
            2 => number switch
            {
                0 => 4.0 * (point.X - 0.5) * (point.X - 1.0) * (point.Y - 1.0 + (point.Y - 0.5)),
                1 => -8.0 * point.X * (point.X - 1.0) * (point.Y - 1.0 + (point.Y - 0.5)),
                2 => 4.0 * point.X * (point.X - 0.5) * (point.Y - 1.0 + (point.Y - 0.5)),
                3 => -8.0 * (point.X - 0.5) * (point.X - 1.0) * (point.Y - 1.0 + point.Y),
                4 => 16.0 * point.X * (point.X - 1.0) * (point.Y - 1.0 + point.Y),
                5 => -8.0 * point.X * (point.X - 0.5) * (point.Y - 1.0 + point.Y),
                6 => 4.0 * (point.X - 0.5) * (point.X - 1.0) * (point.Y - 0.5 + point.Y),
                7 => -8.0 * point.X * (point.X - 1.0) * (point.Y - 0.5 + point.Y),
                8 => 4.0 * point.X * (point.X - 0.5) * (point.Y - 0.5 + point.Y),
                _ => throw new ArgumentOutOfRangeException(nameof(number), number, "Not expected function number!")
            },
            _ => throw new ArgumentOutOfRangeException(nameof(varNumber), varNumber, "Not expected var number!")
        };
}