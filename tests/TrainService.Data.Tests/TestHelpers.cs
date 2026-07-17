using System;
using System.Linq;
using System.Threading.Tasks;
using TrainService.Cad;
using TrainService.Core.Entities;
using TrainService.Data;
using TrainService.App.Services;

namespace TrainService.Data.Tests;

public static class TestHelpers
{
    public static CadDocument DokumaniKur(int katman, int node, int segment, double gridSize)
    {
        var doc = new CadDocument();
        doc.GridSizeMm = gridSize;
        var layers = new System.Collections.Generic.List<TrainService.Core.Entities.CadLayer>();
        for (int i = 0; i < katman; i++)
        {
            var l = new TrainService.Core.Entities.CadLayer { Id = Guid.NewGuid(), Name = $"Layer {i + 1}", DisplayOrder = i };
            layers.Add(l);
        }
        doc.LoadLayers(layers);
        if (layers.Count > 0) doc.SetActiveLayer(layers[0].Id);

        Guid[] nodes = new Guid[node];
        for (int i = 0; i < node; i++)
        {
            var n = new TrackNode
            {
                Id = Guid.NewGuid(),
                LayerId = doc.ActiveLayerId,
                Position = new TrainService.Core.Geometry.Vector2D(i * 1000, 0)
            };
            nodes[i] = n.Id;
            doc.RestoreEntity(n);
        }

        for (int i = 0; i < segment; i++)
        {
            var s = new TrackSegment
            {
                Id = Guid.NewGuid(),
                LayerId = doc.ActiveLayerId,
                StartNodeId = nodes[i],
                EndNodeId = nodes[i + 1],
                LengthMm = 1000.0
            };
            doc.RestoreEntity(s);
        }

        return doc;
    }

    public static async Task<Project> YeniProjeAsync(TempSqliteFixture fx, string ad)
    {
        await using var db = fx.CreateContext();
        var p = new Project { Id = Guid.NewGuid(), Name = ad };
        db.Projects.Add(p);
        await db.SaveChangesAsync();
        return p;
    }

    public static Project YeniProje(TrainDbContext db, string ad)
    {
        var p = new Project { Id = Guid.NewGuid(), Name = ad };
        db.Projects.Add(p);
        return p;
    }

    public static async Task<Project> VarsayilanProjeAsync(TempSqliteFixture fx)
    {
        return await YeniProjeAsync(fx, "Varsayilan");
    }

    public static void KatmanVeGeometriEkle(TrainDbContext db, Project p, int nodeSayisi, int segmentSayisi)
    {
        var l = new Layer { Id = Guid.NewGuid(), ProjectId = p.Id, Name = "L1" };
        db.Layers.Add(l);

        Guid[] nodes = new Guid[nodeSayisi];
        for (int i = 0; i < nodeSayisi; i++)
        {
            var n = new TrackNode
            {
                Id = Guid.NewGuid(),
                ProjectId = p.Id,
                LayerId = l.Id,
                Position = new TrainService.Core.Geometry.Vector2D(i * 1000, 0)
            };
            nodes[i] = n.Id;
            db.TrackNodes.Add(n);
        }

        for (int i = 0; i < segmentSayisi; i++)
        {
            var s = new TrackSegment
            {
                Id = Guid.NewGuid(),
                ProjectId = p.Id,
                LayerId = l.Id,
                StartNodeId = nodes[i],
                EndNodeId = nodes[i + 1],
                LengthMm = 1000
            };
            db.TrackSegments.Add(s);
        }
    }

    public static async Task<(CadDocumentStore, Guid)> YeniStoreVeProjeAsync(TempSqliteFixture fx, string projeAdi = "TestProjesi")
    {
        var db = fx.CreateContext();
        var store = new CadDocumentStore(db);
        var pid = Guid.NewGuid();
        return (store, pid);
    }
}

public static class CadDocumentStoreTestExtensions
{
    // Testler için wrapper metotlar
    public static async Task SaveDocumentAsync(this CadDocumentStore store, Guid projectId, CadDocument doc)
    {
        // Data tests logic often injects a specific project ID. 
        // We ensure the document is saved in the context of our tests.
        // As the current implementation of CadDocumentStore uses a single project assumption (first project),
        // we just call the regular save. If it strictly needs projectId, we update CadDocumentStore.
        await store.SaveDocumentAsync(projectId, doc);
    }

    public static async Task<CadDocument> LoadDocumentAsync(this CadDocumentStore store, Guid projectId)
    {
        var doc = new CadDocument();
        await store.LoadDocumentAsync(projectId, doc);
        return doc;
    }
}
