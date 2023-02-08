namespace Project;

public readonly record struct DirichletBoundary(int Element, int Edge)
{
    public static DirichletBoundary[] ReadJson(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new("File does not exist");
        }

        using var sr = new StreamReader(jsonPath);
        return JsonConvert.DeserializeObject<DirichletBoundary[]>(sr.ReadToEnd()) ??
               throw new NullReferenceException("File with dirichlet boundaries is incorrect!");
    }
}