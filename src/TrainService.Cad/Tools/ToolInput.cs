using TrainService.Cad.Selection;
using TrainService.Cad.UndoRedo;

namespace TrainService.Cad.Tools;

public enum ToolMouseButton { Left, Right, Middle }

public enum ToolKey { Escape, Enter }

/// <summary>
/// Araçlara her olayda geçirilen bağlam. 
/// Araçlar CadDocument'ı DOĞRUDAN mutate edemez; tek yazma yolu Commands.Do(...)'dur.
/// </summary>
public sealed record ToolContext(CadDocument Document, CommandStack Commands, SelectionService Selection);
