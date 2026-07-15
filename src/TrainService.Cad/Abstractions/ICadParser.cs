using System.Collections.Generic;
using System.Threading.Tasks;
using TrainService.Core.Entities;

namespace TrainService.Cad.Abstractions;

public interface ICadParser
{
    Task<(List<TrackNode> Nodes, List<TrackSegment> Segments)> ParseAsync(string filePath);
}
