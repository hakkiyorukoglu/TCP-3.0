using System;
using System.Collections.Generic;
using System.Linq;
using TrainService.Cad.Snapping;
using TrainService.Cad.UndoRedo;
using TrainService.Core.Entities;
using TrainService.Core.Enums;
using TrainService.Core.Geometry;
using TrainService.Core.Topology;

namespace TrainService.Cad.Tools;

public sealed class RouteTool : ITool
{
    public string Name => "Route";
    private readonly List<RouteStep> _adimlar = new();
    private TrackGraph? _graf;
    private Guid _adayId;
    private bool _adayGecerli;
    
    public PreviewShape? Preview { get; private set; }

    public void Activate(ToolContext ctx)
    {
        _adimlar.Clear();
        _adayId = Guid.Empty;
        Preview = null;
        // Graf hızlı-yol için kurulur (K3); commit'te güncel dokümana karşı yeniden doğrulanır.
        _graf = TrackGraph.Build(ctx.Document.Entities.OfType<TrackNode>(),
                                 ctx.Document.Entities.OfType<TrackSegment>());
    }

    public void Deactivate(ToolContext ctx)
    {
        _adimlar.Clear();
        Preview = null;
        _graf = null;
    }

    public void OnPointerMove(SnapResult snapped, ToolContext ctx)
    {
        // SADECE OnSegment snap aday üretir (Gemini'nin doğru kararı — korunuyor):
        if (snapped.Kind == SnapKind.OnSegment && snapped.TargetId is Guid segId
            && ctx.Document.TryGetEntity(segId, out var e) && e is TrackSegment
            && ctx.Document.IsSelectable(segId)) // gizli/kilitli katman rotaya giremez
        {
            _adayId = segId;
            _adayGecerli = AdayGecerliMi(segId);
        }
        else
        {
            _adayId = Guid.Empty;
            _adayGecerli = false;
        }
        Preview = new PreviewRoute(_adimlar, _adayId, _adayGecerli);
    }

    private bool AdayGecerliMi(Guid segId)
    {
        if (_adimlar.Count == 0) return true;                      // ilk adım: her segment olur
        if (_adimlar.Any(a => a.SegmentId == segId)) return false; // aynı segment iki kez giremez (MVP)
        return _graf!.AreAdjacent(_adimlar[^1].SegmentId, segId);  // komşuluk zorunlu
    }

    public void OnPointerDown(SnapResult snapped, ToolMouseButton button, ToolContext ctx)
    {
        if (button == ToolMouseButton.Right) { Commit(ctx); return; } // sağ tık = zinciri bitir (v3.0.18 kuralı)
        if (button != ToolMouseButton.Left) return;
        if (_adayId == Guid.Empty || !_adayGecerli) return;           // geçersiz aday YOK SAYILIR
        if (!ctx.Document.TryGetEntity(_adayId, out var e) || e is not TrackSegment seg) return; // bayat guard

        if (_adimlar.Count == 0)
        {
            _adimlar.Add(new RouteStep(seg.Id, TravelDirection.Forward)); // geçici yön; 2. adımda düzelir
        }
        else
        {
            var onceki = _adimlar[^1];
            ctx.Document.TryGetEntity(onceki.SegmentId, out var pe);
            var prev = (TrackSegment)pe!;
            Guid ortak = OrtakDugum(prev, seg);
            if (ortak == Guid.Empty) return; // komşu değil (guard, olmamalı)

            if (_adimlar.Count == 1) // ★ ilk adımın yönünü ŞİMDİ kesinleştir (K4):
            {
                // önceki adımın ÇIKIŞI ortak düğüm olmalı: Forward→EndNode çıkışsa doğru; değilse Backward.
                var duzeltilmis = new RouteStep(prev.Id,
                    prev.EndNodeId == ortak ? TravelDirection.Forward : TravelDirection.Backward);
                _adimlar[0] = duzeltilmis;
            }
            // Yeni adımın GİRİŞİ ortak düğüm: StartNode girişse Forward, EndNode girişse Backward.
            _adimlar.Add(new RouteStep(seg.Id,
                seg.StartNodeId == ortak ? TravelDirection.Forward : TravelDirection.Backward));
        }
        Preview = new PreviewRoute(_adimlar, Guid.Empty, false);
    }

    private static Guid OrtakDugum(TrackSegment a, TrackSegment b)
    {
        if (a.StartNodeId == b.StartNodeId || a.StartNodeId == b.EndNodeId) return a.StartNodeId;
        if (a.EndNodeId == b.StartNodeId || a.EndNodeId == b.EndNodeId) return a.EndNodeId;
        return Guid.Empty;
    }

    public void OnPointerUp(SnapResult s, ToolMouseButton b, ToolContext c) { }

    public void OnKeyDown(ToolKey key, ToolContext ctx)
    {
        switch (key)
        {
            case ToolKey.Enter: Commit(ctx); break;                    // ★ Enter = commit (DÜZELTME 1)
            case ToolKey.Escape: _adimlar.Clear(); Preview = null; break; // ★ Esc = İPTAL
        }
    }

    private void Commit(ToolContext ctx)
    {
        if (_adimlar.Count == 0) return; // K5: boş rota yok
        // ★ K3/DÜZELTME 3: GÜNCEL dokümana karşı doğrula (bayat graf koruması):
        var guncelGraf = TrackGraph.Build(ctx.Document.Entities.OfType<TrackNode>(),
                                          ctx.Document.Entities.OfType<TrackSegment>());
        var rota = new Route { LayerId = ctx.Document.ActiveLayerId };
        rota.Steps.AddRange(_adimlar);
        if (!guncelGraf.ValidateRoute(rota))
        {
            _adimlar.Clear();
            Preview = null;
            return; // geçersiz → iptal
        }

        rota.CachedBounds = HesaplaBounds(rota, ctx.Document);     // ★ DÜZELTME 2
        ctx.Commands.Do(new AddEntityCommand(rota), ctx.Document); // undo'lu, mevcut komutla
        ctx.Selection.Set(new[] { rota.Id });                      // yeni rota seçili gelir
        _adimlar.Clear();
        Preview = null;
    }

    private static BoundingBox HesaplaBounds(Route r, CadDocument doc)
    {
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var st in r.Steps)
        {
            if (doc.TryGetEntity(st.SegmentId, out var e) && e is TrackSegment s
                && doc.TryGetEntity(s.StartNodeId, out var na) && na is TrackNode a
                && doc.TryGetEntity(s.EndNodeId, out var nb) && nb is TrackNode b)
            {
                minX = Math.Min(minX, Math.Min(a.Position.X, b.Position.X));
                maxX = Math.Max(maxX, Math.Max(a.Position.X, b.Position.X));
                minY = Math.Min(minY, Math.Min(a.Position.Y, b.Position.Y));
                maxY = Math.Max(maxY, Math.Max(a.Position.Y, b.Position.Y));
            }
        }
        return minX > maxX ? default : new BoundingBox(minX, minY, maxX, maxY);
    }
}
