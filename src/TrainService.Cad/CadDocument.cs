using System;
using System.Collections.Generic;
using TrainService.Core.Entities;

namespace TrainService.Cad;

public enum DocumentChangeKind
{
    Added,
    Removed,
    Modified
}

public sealed record DocumentChangedEventArgs(DocumentChangeKind Kind, Guid? EntityId);

public sealed class CadDocument
{
    private readonly Dictionary<Guid, CadEntity> _entities = new();
    private readonly List<CadLayer> _layers = new();

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
        IsDirty = true;
        Changed?.Invoke(this, new DocumentChangedEventArgs(DocumentChangeKind.Added, e.Id));
    }

    internal void RemoveEntity(Guid id)
    {
        if (_entities.Remove(id))
        {
            IsDirty = true;
            Changed?.Invoke(this, new DocumentChangedEventArgs(DocumentChangeKind.Removed, id));
        }
    }

    public bool TryGetEntity(Guid id, out CadEntity e)
    {
        return _entities.TryGetValue(id, out e!);
    }
}
