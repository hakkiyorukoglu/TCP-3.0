using System;
using FluentAssertions;
using Xunit;
using TrainService.Cad;
using TrainService.Core.Geometry;
using TrainService.Core.Entities;
using TrainService.Cad.UndoRedo;


namespace TrainService.Cad.Tests;

public class IsDirtyTests
{
    [Fact]
    public void T1818_IsDirty_Dongusu()
    {
        var doc = new CadDocument();
        doc.IsDirty.Should().BeFalse("yeni belge temiz doğar");

        var stack = new CommandStack();
        var node = new TrackNode { Position = new Vector2D(100, 100), LayerId = doc.ActiveLayerId };
        stack.Do(new AddEntityCommand(node), doc);
        doc.IsDirty.Should().BeTrue("ekleme kirletir");

        doc.MarkSaved();
        doc.IsDirty.Should().BeFalse("kayıt temizler");

        stack.Undo(doc);
        doc.IsDirty.Should().BeTrue("★ UNDO DA KİRLETİR: belge artık disktekinden farklı");

        doc.MarkSaved();
        stack.Redo(doc);
        doc.IsDirty.Should().BeTrue("redo da kirletir");
    }
}
