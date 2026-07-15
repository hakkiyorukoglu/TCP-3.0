using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using TrainService.Cad;
using TrainService.Cad.UndoRedo;
using TrainService.Cad.Selection;
using TrainService.Core.Entities;

namespace TrainService.Cad.Tests;

public class UndoRedoTests
{
    private class DummyCommand : ICadCommand
    {
        public string Description { get; }
        private readonly CadEntity _entity;
        
        public DummyCommand(string desc, CadEntity entity)
        {
            Description = desc;
            _entity = entity;
        }

        public void Execute(CadDocument doc) => doc.AddEntity(_entity);
        public void Undo(CadDocument doc) => doc.RemoveEntity(_entity.Id);
    }

    private class TrackOrderCommand : ICadCommand
    {
        public string Description { get; }
        private readonly Action<string> _onExecute;
        private readonly Action<string> _onUndo;
        
        public TrackOrderCommand(string name, Action<string> onExecute, Action<string> onUndo)
        {
            Description = name;
            _onExecute = onExecute;
            _onUndo = onUndo;
        }

        public void Execute(CadDocument doc) => _onExecute(Description);
        public void Undo(CadDocument doc) => _onUndo(Description);
    }

    [Fact]
    public void Test1_Do_Sets_CanUndoTrue_CanRedoFalse()
    {
        var doc = new CadDocument();
        var stack = new CommandStack();
        var cmd = new DummyCommand("T1", new TrackNode());

        stack.Do(cmd, doc);

        Assert.True(stack.CanUndo);
        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Test2_Undo_Redo_RoundTrip_EntityCount()
    {
        var doc = new CadDocument();
        var stack = new CommandStack();
        var cmd = new DummyCommand("T2", new TrackNode());

        int initialCount = doc.Entities.Count; // 0
        
        stack.Do(cmd, doc);
        Assert.Equal(initialCount + 1, doc.Entities.Count);

        stack.Undo(doc);
        Assert.Equal(initialCount, doc.Entities.Count);

        stack.Redo(doc);
        Assert.Equal(initialCount + 1, doc.Entities.Count);
    }

    [Fact]
    public void Test3_Undo_Then_NewDo_Clears_RedoStack()
    {
        var doc = new CadDocument();
        var stack = new CommandStack();
        var cmd1 = new DummyCommand("T3-1", new TrackNode());
        var cmd2 = new DummyCommand("T3-2", new TrackNode());

        stack.Do(cmd1, doc);
        stack.Undo(doc);
        Assert.True(stack.CanRedo); // Undo yaptık, redo edebilir.

        // Yeni komut Do ediliyor
        stack.Do(cmd2, doc);
        Assert.False(stack.CanRedo); // Redo silinmeli
    }

    [Fact]
    public void Test4_EmptyStack_UndoRedo_Is_NoOp()
    {
        var doc = new CadDocument();
        var stack = new CommandStack();

        // Exception fırlatmamalı
        var exception1 = Record.Exception(() => stack.Undo(doc));
        var exception2 = Record.Exception(() => stack.Redo(doc));

        Assert.Null(exception1);
        Assert.Null(exception2);
    }

    [Fact]
    public void Test5_CompositeCommand_ExecutesInOrder_UndoesInReverseOrder()
    {
        var doc = new CadDocument();
        var stack = new CommandStack();
        var execOrder = new List<string>();
        var undoOrder = new List<string>();

        var cmd1 = new TrackOrderCommand("C1", s => execOrder.Add(s), s => undoOrder.Add(s));
        var cmd2 = new TrackOrderCommand("C2", s => execOrder.Add(s), s => undoOrder.Add(s));
        var cmd3 = new TrackOrderCommand("C3", s => execOrder.Add(s), s => undoOrder.Add(s));

        var composite = new CompositeCadCommand("Comp", new[] { cmd1, cmd2, cmd3 });

        stack.Do(composite, doc);
        
        Assert.Equal(new[] { "C1", "C2", "C3" }, execOrder);
        Assert.Empty(undoOrder);

        stack.Undo(doc);
        Assert.Equal(new[] { "C3", "C2", "C1" }, undoOrder);
    }

    [Fact]
    public void Test6_SelectionService_PruneMissing_FiresOnce()
    {
        var doc = new CadDocument();
        var selection = new SelectionService();
        selection.PruneMissing(doc); // Abone yapıldı

        var entity1 = new TrackNode();
        var entity2 = new TrackNode();
        doc.AddEntity(entity1);
        doc.AddEntity(entity2);

        selection.Set(new[] { entity1.Id, entity2.Id });

        int eventFiredCount = 0;
        selection.SelectionChanged += (s, e) => eventFiredCount++;

        // Act: Document'tan entity1'i sil
        doc.RemoveEntity(entity1.Id);

        // Assert
        Assert.DoesNotContain(entity1.Id, selection.SelectedIds);
        Assert.Contains(entity2.Id, selection.SelectedIds);
        Assert.Equal(1, eventFiredCount);
    }

    [Fact]
    public void Test7_Capacity2_Stack_Drops_OldestCommand()
    {
        var doc = new CadDocument();
        var stack = new CommandStack(capacity: 2);
        
        stack.Do(new DummyCommand("1", new TrackNode()), doc);
        stack.Do(new DummyCommand("2", new TrackNode()), doc);
        stack.Do(new DummyCommand("3", new TrackNode()), doc); // 1. düşmeli

        // Undo 2 defa çalışabilir
        Assert.True(stack.CanUndo);
        stack.Undo(doc); // Undo 3
        
        Assert.True(stack.CanUndo);
        stack.Undo(doc); // Undo 2

        Assert.False(stack.CanUndo); // 1 zaten capacity'den düştüğü için artık undo yapılamaz
    }
}
