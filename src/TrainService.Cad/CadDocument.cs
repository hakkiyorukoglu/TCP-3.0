using System;
using System.Collections.Generic;
using System.Linq;
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
    DocumentReloaded,
    LayerChanged
}

public sealed record DocumentChangedEventArgs(DocumentChangeKind Kind, Guid? EntityId, TrainService.Core.Geometry.BoundingBox? DirtyRegion = null);

public sealed class CadDocument
{
    private readonly Dictionary<Guid, CadEntity> _entities = new();
    private readonly Dictionary<Guid, CadLayer> _layers = new();
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

    public static class SabitKatmanlar
    {
        public static readonly Guid Zemin = new("11111111-0000-0000-0000-000000000000");
        public static readonly Guid AltKat = new("22222222-0000-0000-0000-000000000000");
        public static readonly Guid UstKat = new("33333333-0000-0000-0000-000000000000");
    }

    public CadDocument()
    {
        var zemin = new CadLayer { Id = SabitKatmanlar.Zemin, Name = "Zemin", ZHeightMm = 0, IsVisible = true, DisplayOrder = 0 };
        var altKat = new CadLayer { Id = SabitKatmanlar.AltKat, Name = "Alt Kat", ZHeightMm = -350, IsVisible = true, DisplayOrder = 1 };
        var ustKat = new CadLayer { Id = SabitKatmanlar.UstKat, Name = "Üst Kat", ZHeightMm = 400, IsVisible = true, DisplayOrder = 2 };
        
        _layers[zemin.Id] = zemin;
        _layers[altKat.Id] = altKat;
        _layers[ustKat.Id] = ustKat;
        
        ActiveLayerId = zemin.Id;
    }

    public IReadOnlyCollection<CadEntity> Entities => _entities.Values;
    public IReadOnlyCollection<CadLayer> Layers => _layers.Values;
    public Guid ActiveLayerId { get; private set; }
    public bool IsDirty { get; private set; }
    
    public bool TryGetLayer(Guid id, out CadLayer layer) => _layers.TryGetValue(id, out layer!);
    
    public void SetActiveLayer(Guid id)
    {
        if (_layers.ContainsKey(id)) ActiveLayerId = id;
    }
    
    public bool IsVisible(Guid entityId)
    {
        if (!TryGetEntity(entityId, out var e)) return false;
        if (!TryGetLayer(e.LayerId, out var l)) return true; // güvenlik ağı: katman yoksa görünür kıl
        return l.IsVisible;
    }
    
    public bool IsSelectable(Guid entityId)
    {
        if (!TryGetEntity(entityId, out var e)) return false;
        if (!TryGetLayer(e.LayerId, out var l)) return true; // güvenlik ağı: katman yoksa seçilebilir kıl
        return l.IsVisible && !l.IsLocked;
    }
    
    public void SetLayerVisibility(Guid layerId, bool isVisible)
    {
        if (_layers.TryGetValue(layerId, out var l) && l.IsVisible != isVisible)
        {
            l.IsVisible = isVisible;
            Changed?.Invoke(this, new DocumentChangedEventArgs(DocumentChangeKind.LayerChanged, null));
        }
    }
    
    public void SetLayerLock(Guid layerId, bool isLocked)
    {
        if (_layers.TryGetValue(layerId, out var l) && l.IsLocked != isLocked)
        {
            l.IsLocked = isLocked;
            Changed?.Invoke(this, new DocumentChangedEventArgs(DocumentChangeKind.LayerChanged, null));
        }
    }
    
    public void LoadLayers(IEnumerable<CadLayer> layers, bool replace = false)
    {
        if (replace) _layers.Clear();
        foreach (var l in layers)
        {
            if (_layers.TryGetValue(l.Id, out var existing))
            {
                existing.Name = l.Name;
            }
            else
            {
                var newLayer = new CadLayer 
                { 
                    Id = l.Id, 
                    Name = l.Name, 
                    ZHeightMm = l.ZHeightMm, 
                    DisplayOrder = _layers.Count, 
                    IsVisible = l.IsVisible, 
                    IsLocked = l.IsLocked 
                };
                _layers[l.Id] = newLayer;
            }
        }
        
        if (ActiveLayerId == Guid.Empty && _layers.Count > 0)
        {
            ActiveLayerId = _layers.Values.OrderBy(x => x.DisplayOrder).First().Id;
        }
    }

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


