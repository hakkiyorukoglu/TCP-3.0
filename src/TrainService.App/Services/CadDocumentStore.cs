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
        await _context.Routes.Where(r => r.ProjectId == projectId).ExecuteDeleteAsync();
        await _context.TrackNodes.Where(n => n.ProjectId == projectId).ExecuteDeleteAsync();
        await _context.TrackSegments.Where(s => s.ProjectId == projectId).ExecuteDeleteAsync();
        await _context.Layers.Where(l => l.ProjectId == projectId).ExecuteDeleteAsync();
        await _context.Ramps.Where(r => r.ProjectId == projectId).ExecuteDeleteAsync();
        await _context.Switches.Where(s => s.ProjectId == projectId).ExecuteDeleteAsync();
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
            var existing = await _context.Layers.FindAsync(l.Id);
            if (existing != null)
            {
                existing.ProjectId = projectId;
                existing.Name = l.Name;
            }
            else
            {
                _context.Layers.Add(new Layer { Id = l.Id, ProjectId = projectId, Name = l.Name });
            }
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
            else if (entity is Route route)
            {
                var dbRoute = new Route
                {
                    Id = route.Id,
                    ProjectId = projectId,
                    LayerId = route.LayerId,
                    Name = route.Name
                };
                foreach (var s in route.Steps)
                {
                    dbRoute.Steps.Add(new RouteStep(s.SegmentId, s.Direction));
                }
                _context.Routes.Add(dbRoute);
            }
            else if (entity is RailSwitch sw)
            {
                var dbSw = new RailSwitch
                {
                    Id = sw.Id,
                    ProjectId = projectId,
                    LayerId = sw.LayerId,
                    Position = new TrainService.Core.Geometry.Vector2D(sw.Position.X, sw.Position.Y),
                    RotationDeg = sw.RotationDeg,
                    EntryNodeId = sw.EntryNodeId,
                    MainExitNodeId = sw.MainExitNodeId,
                    DivergingExitNodeId = sw.DivergingExitNodeId,
                    State = sw.State,
                    BoundServoDeviceId = sw.BoundServoDeviceId
                };
                _context.Switches.Add(dbSw);
            }
            else if (entity is Ramp rmp)
            {
                var dbRmp = new Ramp
                {
                    Id = rmp.Id,
                    ProjectId = projectId,
                    LayerId = rmp.LayerId,
                    SegmentId = rmp.SegmentId,
                    Position = new TrainService.Core.Geometry.Vector2D(rmp.Position.X, rmp.Position.Y),
                    RotationDeg = rmp.RotationDeg,
                    EntryNodeId = rmp.EntryNodeId,
                    ExitNodeId = rmp.ExitNodeId,
                    StartZ = rmp.StartZ,
                    EndZ = rmp.EndZ,
                    LengthMm = rmp.LengthMm
                };
                _context.Ramps.Add(dbRmp);
            }
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
        var cadLayers = new System.Collections.Generic.List<CadLayer>();
        foreach (var l in layers)
        {
            cadLayers.Add(new CadLayer { Id = l.Id, Name = l.Name });
        }
        document.LoadLayers(cadLayers, replace: true);

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

        var routes = await _context.Routes.Include(r => r.Steps).Where(s => s.ProjectId == projectId).ToListAsync();
        foreach (var dbRoute in routes)
        {
            var route = new Route
            {
                Id = dbRoute.Id,
                LayerId = dbRoute.LayerId,
                Name = dbRoute.Name
            };
            foreach (var st in dbRoute.Steps) route.Steps.Add(new RouteStep(st.SegmentId, st.Direction));
            
            // CachedBounds Yüklemede Yeniden Hesaplanır
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var st in route.Steps)
            {
                if (document.TryGetEntity(st.SegmentId, out var e) && e is TrackSegment s &&
                    document.TryGetEntity(s.StartNodeId, out var na) && na is TrackNode a &&
                    document.TryGetEntity(s.EndNodeId, out var nb) && nb is TrackNode b)
                {
                    minX = System.Math.Min(minX, System.Math.Min(a.Position.X, b.Position.X));
                    maxX = System.Math.Max(maxX, System.Math.Max(a.Position.X, b.Position.X));
                    minY = System.Math.Min(minY, System.Math.Min(a.Position.Y, b.Position.Y));
                    maxY = System.Math.Max(maxY, System.Math.Max(a.Position.Y, b.Position.Y));
                }
            }
            if (minX <= maxX) route.CachedBounds = new TrainService.Core.Geometry.BoundingBox(minX, minY, maxX, maxY);
            
            document.RestoreEntity(route);
        }

        var sws = await _context.Switches.Where(s => s.ProjectId == projectId).ToListAsync();
        foreach (var dbSw in sws)
        {
            var sw = new RailSwitch
            {
                Id = dbSw.Id,
                LayerId = dbSw.LayerId,
                Position = new TrainService.Core.Geometry.Vector2D(dbSw.Position.X, dbSw.Position.Y),
                RotationDeg = dbSw.RotationDeg,
                EntryNodeId = dbSw.EntryNodeId,
                MainExitNodeId = dbSw.MainExitNodeId,
                DivergingExitNodeId = dbSw.DivergingExitNodeId,
                State = dbSw.State,
                BoundServoDeviceId = dbSw.BoundServoDeviceId
            };
            document.RestoreEntity(sw);
        }

        var rmps = await _context.Ramps.Where(r => r.ProjectId == projectId).ToListAsync();
        foreach (var dbRmp in rmps)
        {
            var rmp = new Ramp
            {
                Id = dbRmp.Id,
                LayerId = dbRmp.LayerId,
                SegmentId = dbRmp.SegmentId,
                Position = new TrainService.Core.Geometry.Vector2D(dbRmp.Position.X, dbRmp.Position.Y),
                RotationDeg = dbRmp.RotationDeg,
                EntryNodeId = dbRmp.EntryNodeId,
                ExitNodeId = dbRmp.ExitNodeId,
                StartZ = dbRmp.StartZ,
                EndZ = dbRmp.EndZ,
                LengthMm = dbRmp.LengthMm
            };
            document.RestoreEntity(rmp);
        }

        document.NotifyReloaded();
        document.MarkSaved();
    }

    public Task CreateSnapshotAsync(CadDocument document, string name)
    {
        return Task.CompletedTask;
    }
}
