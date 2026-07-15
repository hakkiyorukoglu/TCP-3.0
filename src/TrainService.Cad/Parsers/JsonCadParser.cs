using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TrainService.Cad.Abstractions;
using TrainService.Cad.Models;
using TrainService.Core.Entities;

namespace TrainService.Cad.Parsers;

public class JsonCadParser : ICadParser
{
    public async Task<(List<TrackNode> Nodes, List<TrackSegment> Segments)> ParseAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("CAD dosyası bulunamadı.", filePath);

        var json = await File.ReadAllTextAsync(filePath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var exportModel = JsonSerializer.Deserialize<CadExportModel>(json, options);

        if (exportModel == null)
            throw new InvalidOperationException("CAD verisi okunamadı veya JSON geçersiz.");

        var nodes = new List<TrackNode>();
        foreach (var n in exportModel.Nodes)
        {
            nodes.Add(new TrackNode
            {
                Id = Guid.Parse(n.Id),
                Position = new TrainService.Core.Geometry.Vector2D(n.X, n.Y),
                Z = n.Z,
                Role = TrainService.Core.Enums.NodeRole.Plain
            });
        }

        var segments = new List<TrackSegment>();
        foreach (var s in exportModel.Segments)
        {
            segments.Add(new TrackSegment
            {
                Id = Guid.Parse(s.Id),
                StartNodeId = Guid.Parse(s.StartNodeId),
                EndNodeId = Guid.Parse(s.EndNodeId),
                LengthMm = s.LengthMm
            });
        }

        return (nodes, segments);
    }
}
