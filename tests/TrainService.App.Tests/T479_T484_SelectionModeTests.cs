using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using TrainService.Core.Geometry;
using TrainService.Core.Entities;
using TrainService.Cad;
using TrainService.Cad.Tools;
using TrainService.Cad.Selection;
using TrainService.Cad.Snapping;
using TrainService.Cad.UndoRedo;
using TrainService.Cad.Clipboard;
using TrainService.App.Controls.Ribbon;

namespace TrainService.App.Tests;

/// <summary>
/// v3.0.29.21 Selection Modlari testleri.
/// T479–T484: SelectionMode enum, MarqueeSelector, Fence, SelectTool, ViewModel.
/// </summary>
public sealed class T479_T484_SelectionModeTests
{
    private static SnapResult Snap(double x, double y) => new(new Vector2D(x, y), SnapKind.None, null);

    private static ToolContext Ctx(CadDocument d, CommandStack c, SelectionService s, bool add = false)
        => new(d, c, s) { ModifierAdd = add, ClickToleranceWorld = 20 };

    // ============================================================
    // T479: SelectionMode enum varligi ve degerleri
    // ============================================================
    [Fact]
    public void T479_SelectionMode_Enum_Exists_With_Three_Values()
    {
        var type = typeof(SelectionMode);

        type.IsEnum.Should().BeTrue("SelectionMode must be an enum");

        var values = Enum.GetValues(type);
        values.Length.Should().Be(3, "must have Window, Crossing, Fence");

        var names = Enum.GetNames(type);
        names.Should().Contain("Window", "must have Window mode");
        names.Should().Contain("Crossing", "must have Crossing mode");
        names.Should().Contain("Fence", "must have Fence mode");
    }

    // ============================================================
    // T480: MarqueeSelector.WindowSelect() — tamamen iceren
    // ============================================================
    [Fact]
    public void T480_MarqueeSelector_WindowSelect_ContainsOnly()
    {
        var doc = new CadDocument();
        var st = new CommandStack();
        var layerId = CadDocument.SabitKatmanlar.Zemin;

        // Tamamen icerde olacak segment (merkez ~150,150)
        var n1 = new TrackNode { Position = new Vector2D(100, 100), LayerId = layerId };
        var n2 = new TrackNode { Position = new Vector2D(200, 200), LayerId = layerId };
        var segInside = new TrackSegment { StartNodeId = n1.Id, EndNodeId = n2.Id, LayerId = layerId };

        // Kismen disarida olacak segment (merkez ~325,325)
        var n3 = new TrackNode { Position = new Vector2D(250, 250), LayerId = layerId };
        var n4 = new TrackNode { Position = new Vector2D(400, 400), LayerId = layerId };
        var segPartial = new TrackSegment { StartNodeId = n3.Id, EndNodeId = n4.Id, LayerId = layerId };

        // Tamamen disarida olacak segment (merkez ~550,550)
        var n5 = new TrackNode { Position = new Vector2D(500, 500), LayerId = layerId };
        var n6 = new TrackNode { Position = new Vector2D(600, 600), LayerId = layerId };
        var segOutside = new TrackSegment { StartNodeId = n5.Id, EndNodeId = n6.Id, LayerId = layerId };

        foreach (var e in new CadEntity[] { n1, n2, n3, n4, n5, n6, segInside, segPartial, segOutside })
            new AddEntityCommand(e).Execute(doc);

        // Bounding box: (0,0) → (300,300)
        var box = new BoundingBox(0, 0, 300, 300);

        var result = MarqueeSelector.WindowSelect(doc, box);

        result.Should().Contain(segInside.Id, "segment completely inside box should be selected");
        result.Should().NotContain(segPartial.Id, "partially inside segment should NOT be selected in Window mode");
        result.Should().NotContain(segOutside.Id, "completely outside segment should NOT be selected");
    }

    // ============================================================
    // T481: MarqueeSelector.CrossingSelect() — kesisen
    // ============================================================
    [Fact]
    public void T481_MarqueeSelector_CrossingSelect_Intersects()
    {
        var doc = new CadDocument();
        var st = new CommandStack();
        var layerId = CadDocument.SabitKatmanlar.Zemin;

        var n1 = new TrackNode { Position = new Vector2D(100, 100), LayerId = layerId };
        var n2 = new TrackNode { Position = new Vector2D(200, 200), LayerId = layerId };
        var segInside = new TrackSegment { StartNodeId = n1.Id, EndNodeId = n2.Id, LayerId = layerId };

        var n3 = new TrackNode { Position = new Vector2D(250, 250), LayerId = layerId };
        var n4 = new TrackNode { Position = new Vector2D(400, 400), LayerId = layerId };
        var segPartial = new TrackSegment { StartNodeId = n3.Id, EndNodeId = n4.Id, LayerId = layerId };

        var n5 = new TrackNode { Position = new Vector2D(500, 500), LayerId = layerId };
        var n6 = new TrackNode { Position = new Vector2D(600, 600), LayerId = layerId };
        var segOutside = new TrackSegment { StartNodeId = n5.Id, EndNodeId = n6.Id, LayerId = layerId };

        foreach (var e in new CadEntity[] { n1, n2, n3, n4, n5, n6, segInside, segPartial, segOutside })
            new AddEntityCommand(e).Execute(doc);

        var box = new BoundingBox(0, 0, 300, 300);

        var result = MarqueeSelector.CrossingSelect(doc, box);

        result.Should().Contain(segInside.Id, "segment completely inside must be selected");
        result.Should().Contain(segPartial.Id, "partially intersecting segment must be selected in Crossing mode");
        result.Should().NotContain(segOutside.Id, "completely outside segment must NOT be selected");
    }

    // ============================================================
    // T482: MarqueeSelector.FenceSelect() + IsPointInPolygon()
    // ============================================================
    [Fact]
    public void T482a_IsPointInPolygon_Core()
    {
        var poly = new List<Vector2D>
        {
            new(0, 0), new(100, 0), new(100, 100), new(0, 100)
        };

        MarqueeSelector.IsPointInPolygon(new Vector2D(50, 50), poly).Should().BeTrue("center point inside square");
        MarqueeSelector.IsPointInPolygon(new Vector2D(150, 50), poly).Should().BeFalse("point right of square");
        MarqueeSelector.IsPointInPolygon(new Vector2D(-10, 50), poly).Should().BeFalse("point left of square");
        MarqueeSelector.IsPointInPolygon(new Vector2D(50, 150), poly).Should().BeFalse("point above square");
        MarqueeSelector.IsPointInPolygon(new Vector2D(50, -10), poly).Should().BeFalse("point below square");
    }

    [Fact]
    public void T482b_FenceSelect_WithTriangle()
    {
        var doc = new CadDocument();
        var st = new CommandStack();
        var layerId = CadDocument.SabitKatmanlar.Zemin;

        // Icerde olacak segment (merkez ~70,70 — ucgen icinde)
        var n1 = new TrackNode { Position = new Vector2D(60, 60), LayerId = layerId };
        var n2 = new TrackNode { Position = new Vector2D(80, 80), LayerId = layerId };
        var segInside = new TrackSegment { StartNodeId = n1.Id, EndNodeId = n2.Id, LayerId = layerId };

        // Disarida olacak segment (merkez ~225,225 — ucgen disinda)
        var n3 = new TrackNode { Position = new Vector2D(200, 200), LayerId = layerId };
        var n4 = new TrackNode { Position = new Vector2D(250, 250), LayerId = layerId };
        var segOutside = new TrackSegment { StartNodeId = n3.Id, EndNodeId = n4.Id, LayerId = layerId };

        foreach (var e in new CadEntity[] { n1, n2, n3, n4, segInside, segOutside })
            new AddEntityCommand(e).Execute(doc);

        // Ucgen poligon: (0,0) → (200,0) → (100,200)
        var poly = new List<Vector2D>
        {
            new(0, 0), new(200, 0), new(100, 200)
        };

        var result = MarqueeSelector.FenceSelect(doc, poly);

        result.Should().Contain(segInside.Id, "segment inside triangle should be selected");
        result.Should().NotContain(segOutside.Id, "segment outside triangle should NOT be selected");
    }

    // ============================================================
    // T483: SelectTool Fence modu durum gecisleri
    // ============================================================
    [Fact]
    public void T483a_SelectTool_FenceMode_Flow()
    {
        var doc = new CadDocument();
        var st = new CommandStack();
        var sel = new SelectionService();
        var tool = new SelectTool();
        var ctx = Ctx(doc, st, sel);

        // Fence moduna gec
        tool.SetMode(SelectionMode.Fence);
        tool.Name.Should().Be("Select");

        // 1. nokta
        tool.OnPointerDown(Snap(0, 0), ToolMouseButton.Left, ctx);
        tool.Preview.Should().NotBeNull("fence preview should appear after first point");
        tool.Preview.Should().BeOfType<PreviewFence>("preview must be fence type");
        var fence1 = (PreviewFence)tool.Preview!;
        fence1.Points.Should().HaveCount(1);
        fence1.IsClosed.Should().BeFalse();

        // 2. nokta
        tool.OnPointerDown(Snap(100, 0), ToolMouseButton.Left, ctx);
        var fence2 = (PreviewFence)tool.Preview!;
        fence2.Points.Should().HaveCount(2);

        // 3. nokta
        tool.OnPointerDown(Snap(100, 100), ToolMouseButton.Left, ctx);
        var fence3 = (PreviewFence)tool.Preview!;
        fence3.Points.Should().HaveCount(3);

        // Enter = commit poligon
        tool.OnKeyDown(ToolKey.Enter, ctx);
        tool.Preview.Should().BeNull("preview should be cleared after commit");
    }

    [Fact]
    public void T483b_SelectTool_Fence_Cancel()
    {
        var doc = new CadDocument();
        var st = new CommandStack();
        var sel = new SelectionService();
        var tool = new SelectTool();
        var ctx = Ctx(doc, st, sel);

        tool.SetMode(SelectionMode.Fence);

        // 2 nokta ekle
        tool.OnPointerDown(Snap(0, 0), ToolMouseButton.Left, ctx);
        tool.OnPointerDown(Snap(100, 0), ToolMouseButton.Left, ctx);
        tool.Preview.Should().NotBeNull();

        // Esc = iptal
        tool.OnKeyDown(ToolKey.Escape, ctx);
        tool.Preview.Should().BeNull("fence should be cancelled on Escape");
    }

    // ============================================================
    // T484: EditorViewModel.SelectionMode + RibbonDefinition
    // ============================================================
    [Fact]
    public void T484a_SelectionMode_RibbonItems_Exist()
    {
        var allItems = RibbonDefinitions.AllItems.ToList();

        var selWindow = allItems.FirstOrDefault(i => i.Id == "SelWindow");
        selWindow.Should().NotBeNull("Window selection button must exist");
        selWindow!.IsToggle.Should().BeTrue("must be toggle button");

        var selCrossing = allItems.FirstOrDefault(i => i.Id == "SelCrossing");
        selCrossing.Should().NotBeNull("Crossing selection button must exist");
        selCrossing!.IsToggle.Should().BeTrue();

        var selFence = allItems.FirstOrDefault(i => i.Id == "SelFence");
        selFence.Should().NotBeNull("Fence selection button must exist");
        selFence!.IsToggle.Should().BeTrue();
    }

    [Fact]
    public void T484b_SelectionMode_AllItems_UniqueIds()
    {
        var allIds = RibbonDefinitions.AllItems.Select(i => i.Id).ToList();
        var distinctIds = allIds.Distinct().ToList();
        distinctIds.Count.Should().Be(allIds.Count, "all ribbon item IDs must be unique");
    }
}