using System.Collections.Generic;
using System.Linq;

namespace TrainService.App.Controls.Ribbon;

// ———— Data Models ————

public sealed record RibbonItem(
    string Id,
    string Label,
    string IconSymbol,
    string? ShortcutText,
    string? CommandName,
    string? CommandParameter,
    string GroupId = "",
    bool IsToggle = false,
    bool IsEnabled = true
);

public sealed record RibbonGroup(
    string Id,
    string Label,
    List<RibbonItem> Items
);

public sealed record RibbonTab(
    string Id,
    string Label,
    List<RibbonGroup> Groups
);

// ———— Static Definition ————

public static class RibbonDefinitions
{
    public static List<RibbonTab> Tabs { get; } = new()
    {
        new RibbonTab("home", "GİRİŞ", new()
        {
            new RibbonGroup("navigation", "", new()
            {
                new("Select", "Seç", "SelectObject24", "(S)", "SetTool", "Select", IsToggle: true),
                new("MoveNearby", "Taşı", "Move24", "", null, null, IsEnabled: false),
                new("Delete", "Sil", "Delete24", "(Del)", "Delete", null),
            }),
            new RibbonGroup("clipboard", "", new()
            {
                new("Copy", "Kopyala", "Copy24", "(Ctrl+C)", "Copy", null),
                new("Cut", "Kes", "Cut24", "(Ctrl+X)", "Cut", null),
                new("Paste", "Yapıştır", "Paste24", "(Ctrl+V)", "Paste", null),
            }),
            new RibbonGroup("layer", "", new()
            {
                new("LayerSelector", "Katman", "", "", "SetActiveLayer", null),
            }),
        }),
        new RibbonTab("draw", "ÇİZİM", new()
        {
            new RibbonGroup("tools", "", new()
            {
                new("Track", "Ray", "ArrowFlowUpRight24", "(T)", "SetTool", "Track", IsToggle: true),
                new("Route", "Hat", "Directions24", "(R)", "SetTool", "Route", IsToggle: true),
                new("Hybrid", "Hibrit", "MergeType24", "(H)", "SetTool", "Hybrid", IsToggle: true),
                new("Ramp", "Rampa", "ArrowExpandUp24", "", "SetTool", "Ramp", IsToggle: true),
                new("Switch", "Makas", "BranchFork24", "(F8)", "SetTool", "Switch", IsToggle: true),
            }),
        }),
        new RibbonTab("edit", "DÜZEN", new()
        {
            new RibbonGroup("history", "", new()
            {
                new("UndoEdit", "Geri Al", "ArrowUndo24", "(Ctrl+Z)", "Undo", null),
                new("RedoEdit", "Yinele", "ArrowRedo24", "(Ctrl+Y)", "Redo", null),
            }),
            new RibbonGroup("modify", "", new()
            {
                new("DeleteEdit", "Sil", "Delete24", "(Del)", "Delete", null),
                new("SplitSegment", "Böl", "Split24", "", null, null, IsEnabled: false),
            }),
            new RibbonGroup("placeholder", "", new()
            {
                // Boş grup — ilerisi için
            }),
        }),
        new RibbonTab("view", "GÖRÜNÜM", new()
        {
            new RibbonGroup("zoom", "", new()
            {
                new("ZoomExtents", "Sığdır", "ZoomFit24", "(Ctrl+Shift+Z)", "ZoomExtents", null),
                new("ZoomWindow", "Pencere", "ZoomIn24", "(W)", "ZoomWindow", null),
            }),
            new RibbonGroup("display", "", new()
            {
                new("ToggleGrid", "Izgara", "GridDots24", "", "ToggleGrid", null),
                new("ToggleSnap", "Snap", "SnapToGrid24", "(F9)", "ToggleSnap", null),
            }),
        }),
    };

    public static List<RibbonItem> QuickAccessItems { get; } = new()
    {
        new("Save", "Kaydet", "Save24", "(Ctrl+S)", "Save", null),
        new("Undo", "Geri Al", "ArrowUndo24", "(Ctrl+Z)", "Undo", null),
        new("Redo", "Yinele", "ArrowRedo24", "(Ctrl+Y)", "Redo", null),
    };

    public static IEnumerable<RibbonItem> AllItems
        => QuickAccessItems.Concat(Tabs.SelectMany(t => t.Groups.SelectMany(g => g.Items)));
}
