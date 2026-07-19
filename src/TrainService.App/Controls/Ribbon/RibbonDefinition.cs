using System.Collections.Generic;
using System.Linq;

namespace TrainService.App.Controls.Ribbon;

// ———— Data Models ————

public sealed record RibbonItem(
    string Id,
    string Label,
    string IconKind,              // IconPacks kind name (örn: "ContentSave")
    string? ShortcutText,
    string? CommandName,
    string? CommandParameter,
    string GroupId = "",
    bool IsToggle = false,
    bool IsEnabled = true,
    string IconPack = "MaterialDesign"  // IconPacks paket adı (varsayılan: MaterialDesign)
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
                new("Select", "Seç", "CursorDefault", "(S)", "SetTool", "Select", IsToggle: true),
                new("MoveNearby", "Taşı", "ArrowAll", "", null, null, IsEnabled: false),
                new("Delete", "Sil", "TrashCan", "(Del)", "Delete", null),
            }),
            new RibbonGroup("clipboard", "", new()
            {
                new("Copy", "Kopyala", "ContentCopy", "(Ctrl+C)", "Copy", null),
                new("Cut", "Kes", "ContentCut", "(Ctrl+X)", "Cut", null),
                new("Paste", "Yapıştır", "ContentPaste", "(Ctrl+V)", "Paste", null),
            }),
            new RibbonGroup("layer", "", new()
            {
                new("LayerSelector", "Katman", "Layers", "", "SetActiveLayer", null),
            }),
        }),
        new RibbonTab("draw", "ÇİZİM", new()
        {
            new RibbonGroup("tools", "", new()
            {
                new("Track", "Ray", "RailroadLight", "(T)", "SetTool", "Track", IsToggle: true),
                new("Route", "Hat", "MapMarkerPath", "(R)", "SetTool", "Route", IsToggle: true),
                new("Hybrid", "Hibrit", "LayersTripleOutline", "(H)", "SetTool", "Hybrid", IsToggle: true),
                new("Ramp", "Rampa", "TrendingUp", "", "SetTool", "Ramp", IsToggle: true),
                new("Switch", "Makas", "SourceBranch", "(F8)", "SetTool", "Switch", IsToggle: true),
            }),
        }),
        new RibbonTab("edit", "DÜZEN", new()
        {
            new RibbonGroup("history", "", new()
            {
                new("UndoEdit", "Geri Al", "UndoVariant", "(Ctrl+Z)", "Undo", null),
                new("RedoEdit", "Yinele", "RedoVariant", "(Ctrl+Y)", "Redo", null),
            }),
            new RibbonGroup("modify", "", new()
            {
                new("DeleteEdit", "Sil", "TrashCan", "(Del)", "Delete", null),
                new("SplitSegment", "Böl", "ArrowSplitHorizontal", "", null, null, IsEnabled: false),
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
                new("ZoomExtents", "Sığdır", "FitToPageOutline", "(Ctrl+Shift+Z)", "ZoomExtents", null),
                new("ZoomWindow", "Pencere", "MagnifyPlus", "(W)", "ZoomWindow", null),
            }),
            new RibbonGroup("display", "", new()
            {
                new("ToggleGrid", "Izgara", "Grid", "", "ToggleGrid", null),
                new("ToggleSnap", "Snap", "RulerSquare", "(F9)", "ToggleSnap", null),
            }),
        }),
    };

    public static List<RibbonItem> QuickAccessItems { get; } = new()
    {
        new("Save", "Kaydet", "ContentSave", "(Ctrl+S)", "Save", null),
        new("Undo", "Geri Al", "UndoVariant", "(Ctrl+Z)", "Undo", null),
        new("Redo", "Yinele", "RedoVariant", "(Ctrl+Y)", "Redo", null),
    };

    public static IEnumerable<RibbonItem> AllItems
        => QuickAccessItems.Concat(Tabs.SelectMany(t => t.Groups.SelectMany(g => g.Items)));
}
