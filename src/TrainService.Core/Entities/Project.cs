namespace TrainService.Core.Entities;

public class Project
{
    public System.Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double GridSizeMm { get; set; } = 100.0;
}
