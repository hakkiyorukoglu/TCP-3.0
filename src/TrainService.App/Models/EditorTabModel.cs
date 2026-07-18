using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TrainService.Cad;
using TrainService.Cad.Clipboard;
using TrainService.Cad.Selection;
using TrainService.Cad.Snapping;
using TrainService.Cad.UndoRedo;

namespace TrainService.App.Models;

/// <summary>
/// Bir editör sekmesinin izole veri seti.
/// Her sekme kendi CadDocument, CommandStack, SelectionService, vb. içerir.
/// </summary>
public sealed partial class EditorTabModel : ObservableObject
{
    public Guid TabId { get; } = Guid.NewGuid();

    [ObservableProperty]
    private Guid _projectId;

    [ObservableProperty]
    private string _displayName = "Yeni Proje";

    [ObservableProperty]
    private bool _isDirty;

    public CadDocument Document { get; }
    public CommandStack CommandStack { get; }
    public SelectionService SelectionService { get; }
    public SnapEngine SnapEngine { get; }
    public ClipboardService ClipboardService { get; }

    public EditorTabModel(
        Guid projectId,
        CadDocument document,
        CommandStack commandStack,
        SelectionService selectionService,
        SnapEngine snapEngine,
        ClipboardService clipboardService)
    {
        _projectId = projectId;
        Document = document;
        CommandStack = commandStack;
        SelectionService = selectionService;
        SnapEngine = snapEngine;
        ClipboardService = clipboardService;

        // IsDirty dinleme
        Document.Changed += (s, e) =>
        {
            if (e.Kind != global::TrainService.Cad.DocumentChangeKind.DocumentReloaded)
                _isDirty = true;
        };
    }
}