using TrainService.Core.Entities;

namespace TrainService.Cad.UndoRedo;

public sealed class AddEntityCommand : ICadCommand
{
    private readonly CadEntity _entity;

    public AddEntityCommand(CadEntity entity)
    {
        _entity = entity;
    }

    public string Description => $"Ekle: {_entity.GetType().Name}";

    public void Execute(CadDocument doc)
    {
        doc.AddEntity(_entity);
    }

    public void Undo(CadDocument doc)
    {
        doc.RemoveEntity(_entity.Id);
    }
}
