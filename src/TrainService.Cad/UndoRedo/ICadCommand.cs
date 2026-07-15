using System;

namespace TrainService.Cad.UndoRedo;

public interface ICadCommand
{
    string Description { get; }
    void Execute(CadDocument doc);
    void Undo(CadDocument doc);
}
