namespace Project;

public readonly record struct Point2D(double X, double Y)
{
    public static Point2D operator +(Point2D a, Point2D b) => new(a.X + b.X, a.Y + b.Y);

    public static Point2D operator -(Point2D a, Point2D b) => new(a.X - b.X, a.Y - b.Y);

    public static Point2D operator *(Point2D p, double value) => new(p.X * value, p.Y * value);

    public static Point2D operator /(Point2D p, double value) => new(p.X / value, p.Y / value);
}

public readonly record struct Interval(
    [property: JsonProperty("Left border")]
    double LeftBorder,
    [property: JsonProperty("Right border")]
    double RightBorder)
{
    [JsonIgnore] public double Center => (LeftBorder + RightBorder) / 2.0;
    [JsonIgnore] public double Length => Math.Abs(RightBorder - LeftBorder);
}

public readonly record struct Rectangle(Point2D LeftBottom, Point2D RightTop)
{
    public Point2D LeftTop { get; } = new(LeftBottom.X, RightTop.Y);
    public Point2D RightBottom { get; } = new(RightTop.X, LeftBottom.Y);
}