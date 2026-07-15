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
        foreach (var cmd in _commands)
        {
            cmd.Execute(doc);
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
