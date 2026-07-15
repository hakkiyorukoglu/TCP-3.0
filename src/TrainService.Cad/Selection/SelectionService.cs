using System;
using System.Collections.Generic;

namespace TrainService.Cad.Selection;

public sealed class SelectionService
{
    private readonly HashSet<Guid> _selectedIds = new();

    public IReadOnlySet<Guid> SelectedIds => _selectedIds;
    
    public event EventHandler? SelectionChanged;

    public void Set(IEnumerable<Guid> ids)
    {
        _selectedIds.Clear();
        foreach (var id in ids) _selectedIds.Add(id);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Add(Guid id)
    {
        if (_selectedIds.Add(id))
            SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Toggle(Guid id)
    {
        if (_selectedIds.Contains(id))
            _selectedIds.Remove(id);
        else
            _selectedIds.Add(id);
            
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        if (_selectedIds.Count > 0)
        {
            _selectedIds.Clear();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    // Dokümandan silinen nesneler seçimden otomatik düşer
    public void PruneMissing(CadDocument doc)
    {
        doc.Changed += (s, e) =>
        {
            if (e.Kind == DocumentChangeKind.Removed && e.EntityId.HasValue)
            {
                if (_selectedIds.Remove(e.EntityId.Value))
                {
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        };
    }
}
