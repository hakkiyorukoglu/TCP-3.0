using System.Collections.Generic;

namespace TrainService.Cad.Models;

public class CadNodeDto
{
    public string Id { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}

public class CadSegmentDto
{
    public string Id { get; set; } = string.Empty;
    public string StartNodeId { get; set; } = string.Empty;
    public string EndNodeId { get; set; } = string.Empty;
    public double LengthMm { get; set; }
}

public class CadExportModel
{
    public List<CadNodeDto> Nodes { get; set; } = new();
    public List<CadSegmentDto> Segments { get; set; } = new();
}
