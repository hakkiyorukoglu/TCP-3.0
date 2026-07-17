using TrainService.Core.Entities;
using TrainService.Core.Enums;

namespace TrainService.Cad.UndoRedo;

public sealed class SetNodeRoleCommand : ICadCommand
{
    private readonly Guid _nodeId;
    private readonly NodeRole _newRole;
    private NodeRole _oldRole;

    public SetNodeRoleCommand(Guid nodeId, NodeRole newRole)
    {
        _nodeId = nodeId;
        _newRole = newRole;
        _oldRole = NodeRole.Plain; // fallback; Execute'da set edilir
    }

    public string Description => $"Node {_nodeId}: Role -> {_newRole}";

    public void Execute(CadDocument doc)
    {
        if (doc.TryGetEntity(_nodeId, out var e) && e is TrackNode node)
        {
            _oldRole = node.Role;
            node.Role = _newRole;
        }
    }

    public void Undo(CadDocument doc)
    {
        if (doc.TryGetEntity(_nodeId, out var e) && e is TrackNode node)
            node.Role = _oldRole;
    }
}
