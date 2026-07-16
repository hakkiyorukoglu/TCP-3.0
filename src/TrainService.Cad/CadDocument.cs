using System;
using System.Collections.Generic;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;
using TrainService.Cad.Spatial;

namespace TrainService.Cad;

public enum DocumentChangeKind
{
    Added,
    Removed,
    Modified,
    GridChanged,
    DocumentReloaded
}

public sealed record DocumentChangedEventArgs(DocumentChangeKind Kind, Guid? EntityId, TrainService.Core.Geometry.BoundingBox? DirtyRegion = null);

public sealed class CadDocument
{
    private readonly Dictionary<Guid, CadEntity> _entities = new();
    private readonly List<CadLayer> _layers = new();
    private readonly SpatialHash _spatial = new(5000);

    private double _gridSizeMm = 100.0;                        // varsayılan

    public double GridSizeMm
    {
        get => _gridSizeMm;
        set
        {
            if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Grid boyutu pozitif olmalıdır.");
            if (Math.Abs(value - _gridSizeMm) < 1e-9) return;   // gerçek değişiklik yoksa event YOK
            _gridSizeMm = value;
            Changed?.Invoke(this, new DocumentChangedEventArgs(DocumentChangeKind.GridChanged, null));
        }
    }

    public CadDocument()
    {
        // 3 varsayılan katman seed ediliyor
        _layers.Add(new CadLayer { Name = "Zemin", ZHeightMm = 0, IsVisible = true });
        _layers.Add(new CadLayer { Name = "Alt Kat", ZHeightMm = -350, IsVisible = true });
        _layers.Add(new CadLayer { Name = "Üst Kat", ZHeightMm = 400, IsVisible = true });
        ActiveLayerId = _layers[0].Id;
    }

    public IReadOnlyCollection<CadEntity> Entities => _entities.Values;
    public IReadOnlyList<CadLayer> Layers => _layers;
    public Guid ActiveLayerId { get; set; }
    public bool IsDirty { get; private set; }

    public event EventHandler<DocumentChangedEventArgs>? Changed;

    // Mutasyon metotları internal — dışarıdan sadece komutlar değiştirebilir
    // (Şimdilik thread-affinity sözleşmesi gereği sadece UI thread üzerinden mutate edilir.)
    internal void AddEntity(CadEntity e)
    {
        _entities[e.Id] = e;
        var b = GetBounds(e);
        if (b != null) _spatial.Add(e.Id, b.Value);
        IsDirty = true;
        Changed?.Invoke(this, new DocumentChangedEventArgs(DocumentChangeKind.Added, e.Id, b));
    }

    internal void RemoveEntity(Guid id)
    {
        if (_entities.TryGetValue(id, out var removed))
        {
            _entities.Remove(id);
            _spatial.Remove(id);
            IsDirty = true;
            Changed?.Invoke(this, new DocumentChangedEventArgs(DocumentChangeKind.Removed, id, removed.Bounds));
        }
    }

    public void RestoreEntity(CadEntity e)
    {
        _entities[e.Id] = e;
        var b = GetBounds(e);
        if (b != null) _spatial.Add(e.Id, b.Value);
    }

    private BoundingBox? GetBounds(CadEntity e)
    {
        if (e.Bounds != null) return e.Bounds;
        if (e is TrackSegment seg && 
            TryGetEntity(seg.StartNodeId, out var enA) && enA is TrackNode nA &&
            TryGetEntity(seg.EndNodeId, out var enB) && enB is TrackNode nB)
        {
            return new BoundingBox(
                Math.Min(nA.Position.X, nB.Position.X),
                Math.Min(nA.Position.Y, nB.Position.Y),
                Math.Max(nA.Position.X, nB.Position.X),
                Math.Max(nA.Position.Y, nB.Position.Y)
            );
        }
        return null;
    }

    public void Clear()
    {
        _entities.Clear();
        _spatial.Clear();
    }

    public void NotifyReloaded()
    {
        Changed?.Invoke(this, new DocumentChangedEventArgs(DocumentChangeKind.DocumentReloaded, null));
    }

    public void MarkSaved()
    {
        IsDirty = false;
        Changed?.Invoke(this, new DocumentChangedEventArgs(DocumentChangeKind.DocumentReloaded, null)); // optional depending on exact requirements
    }

    public bool TryGetEntity(Guid id, out CadEntity e)
    {
        return _entities.TryGetValue(id, out e!);
    }

    public void QueryRegion(TrainService.Core.Geometry.BoundingBox box, List<Guid> reuseBuffer)
    {
        reuseBuffer.Clear();
        _spatial.Query(box, reuseBuffer);
    }
}


