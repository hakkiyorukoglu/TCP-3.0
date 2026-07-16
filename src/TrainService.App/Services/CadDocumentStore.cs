using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrainService.Cad;
using TrainService.Cad.Persistence;
using TrainService.Core.Entities;
using TrainService.Data;

namespace TrainService.App.Services;

public class CadDocumentStore : ICadDocumentStore
{
    private readonly TrainDbContext _context;

    public CadDocumentStore(TrainDbContext context)
    {
        _context = context;
    }

    public async Task SaveDocumentAsync(System.Guid projectId, CadDocument document)
    {
        await _context.TrackNodes.Where(n => n.ProjectId == projectId).ExecuteDeleteAsync();
        await _context.TrackSegments.Where(s => s.ProjectId == projectId).ExecuteDeleteAsync();
        await _context.Layers.Where(l => l.ProjectId == projectId).ExecuteDeleteAsync();
        _context.ChangeTracker.Clear();

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null)
        {
            project = new Project { Id = projectId, Name = "Default", GridSizeMm = document.GridSizeMm };
            _context.Projects.Add(project);
        }
        else
        {
            project.GridSizeMm = document.GridSizeMm;
        }

        foreach (var l in document.Layers)
        {
            _context.Layers.Add(new Layer { Id = l.Id, ProjectId = projectId, Name = l.Name });
        }

        foreach (var entity in document.Entities)
        {
            if (entity is TrackNode node)
            {
                var dbNode = new TrackNode
                {
                    Id = node.Id,
                    ProjectId = projectId,
                    LayerId = node.LayerId,
                    Position = new TrainService.Core.Geometry.Vector2D(node.Position.X, node.Position.Y),
                    Z = node.Z,
                    Role = node.Role
                };
                foreach (var s in node.ConnectedSegments)
                {
                    dbNode.ConnectedSegments.Add(s);
                }
                _context.TrackNodes.Add(dbNode);
            }
            else if (entity is TrackSegment seg)
            {
                var dbSeg = new TrackSegment
                {
                    Id = seg.Id,
                    ProjectId = projectId,
                    LayerId = seg.LayerId,
                    StartNodeId = seg.StartNodeId,
                    EndNodeId = seg.EndNodeId,
                    LengthMm = seg.LengthMm
                };
                _context.TrackSegments.Add(dbSeg);
            }
            // İhtiyaç varsa diğer entity'leri de buraya ekleyebiliriz (Ramp vs).
        }

        await _context.SaveChangesAsync();
    }

    public async Task LoadDocumentAsync(System.Guid projectId, CadDocument document)
    {
        document.Clear();
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project != null)
        {
            document.GridSizeMm = project.GridSizeMm;
        }

        var layers = await _context.Layers.Where(l => l.ProjectId == projectId).ToListAsync();
        ((System.Collections.Generic.List<CadLayer>)document.Layers).Clear();
        foreach (var l in layers)
        {
            ((System.Collections.Generic.List<CadLayer>)document.Layers).Add(new CadLayer { Id = l.Id, Name = l.Name });
        }

        var nodes = await _context.TrackNodes.Where(n => n.ProjectId == projectId).ToListAsync();
        foreach (var dbNode in nodes)
        {
            var trackNode = new TrackNode
            {
                Id = dbNode.Id,
                LayerId = dbNode.LayerId,
                Position = new TrainService.Core.Geometry.Vector2D(dbNode.Position.X, dbNode.Position.Y),
                Z = dbNode.Z,
                Role = dbNode.Role
            };
            document.RestoreEntity(trackNode);
            
            // ConnectedSegments listesini eklemek için:
            foreach(var s in dbNode.ConnectedSegments)
                trackNode.ConnectedSegments.Add(s);
        }

        var segments = await _context.TrackSegments.Where(s => s.ProjectId == projectId).ToListAsync();
        foreach (var dbSeg in segments)
        {
            var trackSeg = new TrackSegment
            {
                Id = dbSeg.Id,
                LayerId = dbSeg.LayerId,
                StartNodeId = dbSeg.StartNodeId,
                EndNodeId = dbSeg.EndNodeId,
                LengthMm = dbSeg.LengthMm
            };
            document.RestoreEntity(trackSeg);
        }

        document.NotifyReloaded();
        document.MarkSaved();
    }

    public Task CreateSnapshotAsync(CadDocument document, string name)
    {
        return Task.CompletedTask;
    }
}
