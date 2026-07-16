using System;
using System.Collections.Generic;

namespace TrainService.Cad.UndoRedo;

public sealed class CompositeCadCommand : ICadCommand
{
    private readonly List<ICadCommand> _commands = new();
    
    public string Description { get; }

    public CompositeCadCommand(string description, IEnumerable<ICadCommand> commands)
    {
        Description = description;
        _commands.AddRange(commands);
    }

    public void Execute(CadDocument doc)
    {
        int executedCount = 0;
        try
        {
            for (int i = 0; i < _commands.Count; i++)
            {
                _commands[i].Execute(doc);
                executedCount++;
            }
        }
        catch
        {
            // Rollback previously executed commands in reverse order
            for (int i = executedCount - 1; i >= 0; i--)
            {
                _commands[i].Undo(doc);
            }
            throw;
        }
    }

    public void Undo(CadDocument doc)
    {
        for (int i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo(doc);
        }
    }
}
