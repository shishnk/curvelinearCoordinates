namespace Project;

public class Point2DJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => typeof(Point2D) == objectType;

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.StartArray)
        {
            var array = JArray.Load(reader);
            if (array.Count == 2) return new Point2D(array[0].Value<double>(), array[1].Value<double>());
            throw new FormatException($"Wrong vector length({array.Count})!");
        }

        if (Point2D.TryParse((string?)reader.Value ?? "", out var point)) return point;
        throw new FormatException($"Can't parse({(string?)reader.Value}) as Point2D!");
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        value ??= new Point2D();
        var p = (Point2D)value;
        writer.WriteRawValue($"[{p.X}, {p.Y}]");
        // [[0, 0],[0, 0]] // runtime exception if use method WriteRaw()
        // [[0, 0][0, 0]]
    }
}

[JsonConverter(typeof(Point2DJsonConverter))]
public readonly record struct Point2D(double X, double Y)
{
    public static bool TryParse(string line, out Point2D point)
    {
        var words = line.Split(new[] { ' ', ',', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 3 || !double.TryParse(words[1], out var x) || !double.TryParse(words[2], out var y))
        {
            point = default;
            return false;
        }

        point = new(x, y);
        return true;
    }

    public static Point2D operator +(Point2D a, Point2D b) => new(a.X + b.X, a.Y + b.Y);

    public static Point2D operator -(Point2D a, Point2D b) => new(a.X - b.X, a.Y - b.Y);

    public static Point2D operator *(Point2D p, double value) => new(p.X * value, p.Y * value);

    public static Point2D operator /(Point2D p, double value) => new(p.X / value, p.Y / value);

    public static double operator *(Point2D a, Point2D b) => a.X * b.X + a.Y * b.Y;

    public static implicit operator Point2D((double, double) tuple) => new(tuple.Item1, tuple.Item2);
}

public class IntervalJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => typeof(Interval) == objectType;

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.StartArray)
        {
            var array = JArray.Load(reader);
            if (array.Count == 2) return new Interval(array[0].Value<double>(), array[1].Value<double>());
            throw new FormatException($"Wrong vector length({array.Count})!");
        }

        if (Interval.TryParse((string?)reader.Value ?? "", out var interval)) return interval;
        throw new FormatException($"Can't parse({(string?)reader.Value}) as Interval!");
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        value ??= new Interval();
        var interval = (Interval)value;
        serializer.Serialize(writer, interval);
    }
}

[JsonConverter(typeof(IntervalJsonConverter))]
public readonly record struct Interval(
    [property: JsonProperty("Left border")]
    double LeftBorder,
    [property: JsonProperty("Right border")]
    double RightBorder)
{
    [JsonIgnore] public double Center => (LeftBorder + RightBorder) / 2.0;
    [JsonIgnore] public double Length => Math.Abs(RightBorder - LeftBorder);

    public static bool TryParse(string line, out Interval interval)
    {
        var words = line.Split(new[] { ' ', ',', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 2 || !float.TryParse(words[0], out var x) || !float.TryParse(words[1], out var y))
        {
            interval = default;
            return false;
        }

        interval = new(x, y);
        return true;
    }
}

public readonly record struct Rectangle(Point2D LeftBottom, Point2D RightTop)
{
    public Point2D LeftTop { get; } = new(LeftBottom.X, RightTop.Y);
    public Point2D RightBottom { get; } = new(RightTop.X, LeftBottom.Y);
}

public class FiniteElement
{
    public IReadOnlyList<int> Nodes { get; }
    public int AreaNumber { get; }
    public double Lambda { get; }

    public FiniteElement(IReadOnlyList<int> nodes, int areaNumber, double lambda)
        => (Nodes, AreaNumber, Lambda) = (nodes, areaNumber, lambda);
}