namespace NovaBank;

public record DataPlan(string Name, decimal Price);

public static class DataPlans
{
    public static readonly List<DataPlan> All = new()
    {
        new("500MB - 7 days", 500m),
        new("1GB - 30 days", 1000m),
        new("2GB - 30 days", 1500m),
        new("5GB - 30 days", 3000m),
        new("10GB - 30 days", 5000m)
    };
}