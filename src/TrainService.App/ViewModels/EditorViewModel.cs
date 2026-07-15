using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TrainService.Core.Abstractions;
using TrainService.Cad;
using TrainService.Cad.UndoRedo;
using TrainService.Cad.Selection;
using TrainService.Cad.Debug;

namespace TrainService.App.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    private readonly ILogBus _logBus;
    
    public CadDocument Document { get; }
    public CommandStack CommandStack { get; }
    public SelectionService SelectionService { get; }

    [ObservableProperty]
    private string _cursorWorldPosition = "0.0, 0.0 mm";

    public EditorViewModel(
        CadDocument document, 
        CommandStack commandStack, 
        SelectionService selectionService,
        ILogBus logBus)
    {
        Document = document;
        CommandStack = commandStack;
        SelectionService = selectionService;
        _logBus = logBus;

        CommandStack.StackChanged += (s, e) =>
        {
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        };
        
        SelectionService.PruneMissing(Document);
    }

    private bool CanUndo() => CommandStack.CanUndo;
    
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        var desc = CommandStack.PeekUndoDescription;
        CommandStack.Undo(Document);
        _logBus.Info("Editor", $"Geri alındı: {desc}");
    }

    private bool CanRedo() => CommandStack.CanRedo;
    
    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        var desc = CommandStack.PeekRedoDescription;
        CommandStack.Redo(Document);
        _logBus.Success("Editor", $"Tekrar yapıldı: {desc}");
    }

    [RelayCommand]
    private void DebugAddLine()
    {
        var cmd = new DebugAddLineCommand(Document.ActiveLayerId);
        CommandStack.Do(cmd, Document);
        _logBus.Success("Editor", $"Komut: {cmd.Description}");
    }
}
