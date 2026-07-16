using System;
using System.Collections.Generic;
using TrainService.Core.Entities;

namespace TrainService.Cad.UndoRedo;

/// <summary>
/// Seçili nesneleri siler. Undo ile geri ekler.
/// MVP sınırı: sadece seçili olanları sil; bağlı segment/node tutarlılık denetimi v3.0.32'de.
/// </summary>
public sealed class DeleteEntitiesCommand : ICadCommand
{
    private readonly IReadOnlyList<Guid> _ids;
    private readonly List<CadEntity> _silinen = new();

    public DeleteEntitiesCommand(IReadOnlyList<Guid> ids) => _ids = ids;

    public string Description => $"Sil: {_ids.Count} nesne";

    public void Execute(CadDocument doc)
    {
        _silinen.Clear();
        foreach (var id in _ids)
        {
            if (doc.TryGetEntity(id, out var e))
            {
                _silinen.Add(e);
                doc.RemoveEntity(id);
            }
        }
    }

    public void Undo(CadDocument doc)
    {
        foreach (var e in _silinen)
            doc.AddEntity(e);
    }
}
