using System;
using System.Collections.Generic;
using TrainService.Cad.Clipboard;
using TrainService.Core.Entities;

namespace TrainService.Cad.UndoRedo;

public sealed class CutEntitiesCommand : ICadCommand
{
    private readonly IReadOnlyList<Guid> _ids;
    private readonly ClipboardService _clipboard;
    private readonly List<CadEntity> _silinen = new();

    public CutEntitiesCommand(IReadOnlyList<Guid> ids, ClipboardService cb) { _ids = ids; _clipboard = cb; }
    public string Description => $"Kes: {_ids.Count} nesne";

    public void Execute(CadDocument doc)
    {
        _silinen.Clear();
        foreach (var id in _ids)
            if (doc.TryGetEntity(id, out var e)) { _silinen.Add(e); doc.RemoveEntity(id); }
        _clipboard.Set(_silinen);
    }

    public void Undo(CadDocument doc)
    {
        foreach (var e in _silinen) doc.AddEntity(e);
    }
}
