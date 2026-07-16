using TrainService.Core.Entities;

namespace TrainService.Cad.UndoRedo;

public sealed class RemoveEntityCommand : ICadCommand
{
    private readonly CadEntity _entity;

    public RemoveEntityCommand(CadEntity entity)
    {
        _entity = entity;
    }

    public string Description => $"Sil: {_entity.GetType().Name}";

    public void Execute(CadDocument doc)
    {
        doc.RemoveEntity(_entity.Id);
    }

    public void Undo(CadDocument doc)
    {
        doc.AddEntity(_entity);
    }
}
