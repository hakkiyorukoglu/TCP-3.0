using TrainService.Cad.Selection;
using TrainService.Cad.UndoRedo;

namespace TrainService.Cad.Tools;

public enum ToolMouseButton { Left, Right, Middle }

public enum ToolKey { Escape, Enter, Delete }

/// <summary>
/// Araçlara her olayda geçirilen bağlam. 
/// Araçlar CadDocument'ı DOĞRUDAN mutate edemez; tek yazma yolu Commands.Do(...)'dur.
/// </summary>
public sealed record ToolContext(CadDocument Document, CommandStack Commands, SelectionService Selection)
{
    public bool ModifierAdd { get; init; }
    public double ClickToleranceWorld { get; init; } = 50.0;
}
