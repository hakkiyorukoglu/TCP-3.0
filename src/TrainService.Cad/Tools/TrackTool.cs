using System;
using System.Collections.Generic;
using TrainService.Cad.Snapping;
using TrainService.Cad.UndoRedo;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Tools;

public sealed class TrackTool : ITool
{
    private enum State { Idle, Chaining }
    private State _state = State.Idle;
    private TrackNode? _chainTail;
    private bool _chainTailIsCommitted;
    private Vector2D _cursor;

    public string Name => "Track";
    public PreviewShape? Preview { get; private set; }

    public void Activate(ToolContext ctx)
    {
        Reset();
    }

    public void Deactivate(ToolContext ctx)
    {
        Reset();
    }

    private void Reset()
    {
        _state = State.Idle;
        _chainTail = null;
        _chainTailIsCommitted = false;
        Preview = null;
    }

    public void OnPointerMove(SnapResult snapped, ToolContext ctx)
    {
        _cursor = snapped.Point;
        if (_state == State.Idle)
        {
            Preview = null;
        }
        else if (_state == State.Chaining && _chainTail != null)
        {
            double dist = (_cursor - _chainTail.Position).Length;
            Preview = new PreviewLine(_chainTail.Position, _cursor, IsValid: dist > 1e-6);
        }
    }

    public void OnPointerDown(SnapResult snapped, ToolMouseButton button, ToolContext ctx)
    {
        if (button == ToolMouseButton.Right)
        {
            if (_state == State.Chaining)
            {
                // Zinciri bitir
                Reset();
                Console.WriteLine("Info: Ray zinciri tamamlandı");
            }
            return;
        }

        if (button == ToolMouseButton.Left)
        {
            if (_state == State.Idle)
            {
                _chainTail = new TrackNode
                {
                    Position = snapped.Point,
                    LayerId = ctx.Document.ActiveLayerId
                };
                _chainTailIsCommitted = false;
                _state = State.Chaining;
                Console.WriteLine($"Info: Ray çizimi başladı: ({snapped.Point.X}, {snapped.Point.Y})");
            }
            else if (_state == State.Chaining && _chainTail != null)
            {
                double dist = (snapped.Point - _chainTail.Position).Length;
                if (dist <= 1e-6)
                {
                    Console.WriteLine("Info: Sıfır uzunluklu segment yoksayıldı");
                    return;
                }

                var newNode = new TrackNode
                {
                    Position = snapped.Point,
                    LayerId = ctx.Document.ActiveLayerId
                };

                var segment = new TrackSegment
                {
                    StartNodeId = _chainTail.Id,
                    EndNodeId = newNode.Id,
                    LengthMm = dist,
                    LayerId = ctx.Document.ActiveLayerId
                };

                var commands = new List<ICadCommand>();
                if (!_chainTailIsCommitted)
                {
                    commands.Add(new AddEntityCommand(_chainTail));
                }
                commands.Add(new AddEntityCommand(newNode));
                commands.Add(new AddEntityCommand(segment));

                var composite = new CompositeCadCommand("Segment Eklendi", commands);
                ctx.Commands.Do(composite, ctx.Document);

                _chainTail = newNode;
                _chainTailIsCommitted = true;
                
                // Preview'ı anlık olarak bozmamak için imleçle tekrar hesapla
                Preview = new PreviewLine(_chainTail.Position, _cursor, IsValid: (_cursor - _chainTail.Position).Length > 1e-6);
                
                Console.WriteLine($"Success: Segment eklendi: L={dist} mm");
            }
        }
    }

    public void OnKeyDown(ToolKey key, ToolContext ctx)
    {
        if (key == ToolKey.Escape)
        {
            if (_state == State.Chaining)
            {
                Reset();
                Console.WriteLine("Info: Çizim iptal edildi");
            }
        }
    }
}
