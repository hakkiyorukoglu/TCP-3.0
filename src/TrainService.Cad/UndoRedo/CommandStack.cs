using System;
using System.Collections.Generic;

namespace TrainService.Cad.UndoRedo;

public sealed class CommandStack
{
    private readonly int _capacity;
    private readonly LinkedList<ICadCommand> _undoStack = new();
    private readonly LinkedList<ICadCommand> _redoStack = new();

    // 0 = sınırsız
    public CommandStack(int capacity = 0)
    {
        _capacity = capacity;
    }

    public event EventHandler? StackChanged;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public string? PeekUndoDescription => CanUndo ? _undoStack.Last?.Value.Description : null;
    public string? PeekRedoDescription => CanRedo ? _redoStack.Last?.Value.Description : null;

    public void Do(ICadCommand cmd, CadDocument doc)
    {
        cmd.Execute(doc);
        
        _undoStack.AddLast(cmd);
        _redoStack.Clear(); // Yeni bir komut çalıştırılınca redo yığını sıfırlanır

        if (_capacity > 0 && _undoStack.Count > _capacity)
        {
            _undoStack.RemoveFirst();
        }

        StackChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Undo(CadDocument doc)
    {
        if (!CanUndo) return; // no-op

        var cmd = _undoStack.Last!.Value;
        _undoStack.RemoveLast();
        
        cmd.Undo(doc);
        
        _redoStack.AddLast(cmd);
        StackChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo(CadDocument doc)
    {
        if (!CanRedo) return; // no-op

        var cmd = _redoStack.Last!.Value;
        _redoStack.RemoveLast();
        
        cmd.Execute(doc);
        
        _undoStack.AddLast(cmd);
        StackChanged?.Invoke(this, EventArgs.Empty);
    }
}
