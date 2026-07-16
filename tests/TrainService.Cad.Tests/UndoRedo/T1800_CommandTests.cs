using System;
using System.Linq;
using FluentAssertions;
using TrainService.Cad;
using TrainService.Cad.UndoRedo;
using TrainService.Core.Entities;
using Xunit;

namespace TrainService.Cad.Tests.UndoRedo;

public class T1800_CommandTests
{
    [Fact]
    public void T1801_AddEntity_ExecuteUndo_Simetri()
    {
        var doc = new CadDocument();
        var node = new TrackNode { Position = new Core.Geometry.Vector2D(10, 20) };
        var cmd = new AddEntityCommand(node);

        // Execute
        cmd.Execute(doc);
        doc.Entities.Should().ContainSingle();
        doc.Entities.Single().Id.Should().Be(node.Id);

        // Undo
        cmd.Undo(doc);
        doc.Entities.Should().BeEmpty();

        // Redo (Execute)
        cmd.Execute(doc);
        doc.Entities.Should().ContainSingle();
        doc.Entities.Single().Id.Should().Be(node.Id);
    }

    [Fact]
    public void T1802_RemoveEntity_Undo_AyniNesneyiGeriGetirir()
    {
        var doc = new CadDocument();
        var node = new TrackNode { Position = new Core.Geometry.Vector2D(10, 20) };
        
        // Setup - add first
        doc.AddEntity(node);
        doc.Entities.Should().ContainSingle();

        var cmd = new RemoveEntityCommand(node);

        // Execute
        cmd.Execute(doc);
        doc.Entities.Should().BeEmpty();

        // Undo
        cmd.Undo(doc);
        doc.Entities.Should().ContainSingle();
        doc.Entities.Single().Should().BeSameAs(node); // Referans korunur
    }
}
