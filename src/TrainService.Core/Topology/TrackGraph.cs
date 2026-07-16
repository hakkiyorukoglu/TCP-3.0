using System;
using System.Collections.Generic;
using System.Linq;
using TrainService.Core.Entities;
using TrainService.Core.Enums;

namespace TrainService.Core.Topology;

/// <summary>
/// Ray ağının mantıksal (matematiksel) grafı. Saf veri — WPF/CadDocument bilmez.
/// Pahalı analiz olduğu için İHTİYAÇ ANINDA Build() ile kurulur, CadDocument'a gömülmez.
/// Roadmap 2.3: "simülasyonun ve blok sinyalizasyonun tek veri kaynağı".
/// </summary>
public sealed class TrackGraph
{
    private readonly Dictionary<Guid, TrackNode> _nodes = new();
    private readonly Dictionary<Guid, TrackSegment> _segments = new();
    // Düğüm -> o düğüme bağlı segment Id'leri (komşuluk listesi / adjacency)
    private readonly Dictionary<Guid, List<Guid>> _adjacency = new();

    public IReadOnlyDictionary<Guid, TrackNode> Nodes => _nodes;
    public IReadOnlyDictionary<Guid, TrackSegment> Segments => _segments;

    private TrackGraph() { }

    /// <summary>Düğüm ve segmentlerden graf kurar. O(N+M). Tekrar çağrılırsa yeni graf döner (immutable kullanım).</summary>
    public static TrackGraph Build(IEnumerable<TrackNode> nodes, IEnumerable<TrackSegment> segments)
    {
        var g = new TrackGraph();
        foreach (var n in nodes)
        {
            g._nodes[n.Id] = n;
            if (!g._adjacency.ContainsKey(n.Id)) g._adjacency[n.Id] = new List<Guid>();
        }
        foreach (var s in segments)
        {
            g._segments[s.Id] = s;
            // Segment iki düğümü birbirine bağlar; adjacency her iki uçtan da eklenir.
            // Düğüm graf'ta yoksa (bozuk veri) atla — Build çökmemeli (guard, AGENTS.md 5/4).
            if (g._adjacency.TryGetValue(s.StartNodeId, out var la)) la.Add(s.Id);
            if (g._adjacency.TryGetValue(s.EndNodeId,   out var lb)) lb.Add(s.Id);
        }
        return g;
    }

    /// <summary>Bir düğüme bağlı segmentlerin Id'leri. Düğüm yoksa BOŞ (exception değil).</summary>
    public IReadOnlyList<Guid> GetAdjacentSegments(Guid nodeId)
        => _adjacency.TryGetValue(nodeId, out var list) ? list : Array.Empty<Guid>();

    /// <summary>Düğümün derecesi = bağlı segment sayısı (blok sınırı tespiti için).</summary>
    public int Degree(Guid nodeId) => GetAdjacentSegments(nodeId).Count;

    /// <summary>İki segment ortak bir düğüm paylaşıyor mu? (Rota adım geçişi doğrulaması)</summary>
    public bool AreAdjacent(Guid segA, Guid segB)
    {
        if (!_segments.TryGetValue(segA, out var a) || !_segments.TryGetValue(segB, out var b)) return false;
        if (segA == segB) return false;
        return a.StartNodeId == b.StartNodeId || a.StartNodeId == b.EndNodeId
            || a.EndNodeId   == b.StartNodeId || a.EndNodeId   == b.EndNodeId;
    }

    /// <summary>
    /// Rotanın adımları fiziksel olarak uç-uca bağlı mı? Ardışık her segment çifti komşu olmalı;
    /// ayrıca yön (Direction) tutarlılığı kontrol edilir: bir adımın ÇIKIŞ düğümü, sonraki adımın
    /// GİRİŞ düğümü olmalı. Kopukluk veya ters yön varsa false.
    /// </summary>
    public bool ValidateRoute(Route route)
    {
        if (route.Steps.Count == 0) return false;
        // Tek adımlı rota geçerlidir (o segment var olduğu sürece).
        for (int i = 0; i < route.Steps.Count; i++)
        {
            var step = route.Steps[i];
            if (!_segments.TryGetValue(step.SegmentId, out var seg)) return false;  // olmayan segment

            if (i == 0) continue;
            var prevStep = route.Steps[i - 1];
            if (!_segments.TryGetValue(prevStep.SegmentId, out var prevSeg)) return false;

            // Önceki adımın ÇIKIŞ düğümü (yönüne göre) = bu adımın GİRİŞ düğümü (yönüne göre) olmalı.
            Guid prevExit  = ExitNode(prevSeg, prevStep.Direction);
            Guid thisEntry = EntryNode(seg, step.Direction);
            if (prevExit != thisEntry) return false;   // kopuk veya ters yön
        }
        return true;
    }

    // Forward = Start->End yürüyüş. Exit = bittiği düğüm; Entry = başladığı düğüm.
    private static Guid EntryNode(TrackSegment s, TravelDirection d)
        => d == TravelDirection.Forward ? s.StartNodeId : s.EndNodeId;
    private static Guid ExitNode(TrackSegment s, TravelDirection d)
        => d == TravelDirection.Forward ? s.EndNodeId : s.StartNodeId;

    /// <summary>
    /// Ağı bloklara böler. Blok = derece≠2 sınır düğümleri arasındaki kesintisiz segment zinciri.
    /// Her segment tam BİR bloğa aittir. O(N+M). Sonuç deterministik (segment Id sırasına göre).
    /// </summary>
    public IReadOnlyList<TrackBlock> FindBlocks()
    {
        var ziyaret = new HashSet<Guid>();          // işlenen segmentler
        var bloklar = new List<TrackBlock>();

        // Deterministik sıra için segmentleri Id'ye göre sırala (test tekrarlanabilirliği).
        foreach (var segId in _segments.Keys.OrderBy(x => x))
        {
            if (ziyaret.Contains(segId)) continue;
            var seg = _segments[segId];

            // Bu segmentten iki yöne büyüyerek maksimal kesintisiz zinciri topla.
            var zincir = new LinkedList<Guid>();
            zincir.AddFirst(segId);
            ziyaret.Add(segId);

            // İleri (EndNode yönünde) büyüt:
            GrowChain(seg.EndNodeId, seg.Id, ziyaret, zincir, appendToEnd: true);
            // Geri (StartNode yönünde) büyüt:
            GrowChain(seg.StartNodeId, seg.Id, ziyaret, zincir, appendToEnd: false);

            var segIds = zincir.ToList();
            var (startN, endN) = ZincirUclari(segIds);
            bloklar.Add(new TrackBlock(Guid.NewGuid(), segIds, startN, endN));
        }
        return bloklar;
    }

    // Bir düğümden devam ederek zinciri büyütür — YALNIZCA derece-2 düğümlerden geçer.
    private void GrowChain(Guid nodeId, Guid gelinenSeg, HashSet<Guid> ziyaret,
                           LinkedList<Guid> zincir, bool appendToEnd)
    {
        while (true)
        {
            // Sınır düğümü (derece≠2) → blok burada biter.
            if (Degree(nodeId) != 2) return;
            // Derece-2: bu düğümden gelinen-dışı TEK bir segment var.
            Guid sonraki = Guid.Empty;
            foreach (var sId in GetAdjacentSegments(nodeId))
                if (sId != gelinenSeg) { sonraki = sId; break; }
            if (sonraki == Guid.Empty || ziyaret.Contains(sonraki)) return;   // döngü/çıkmaz koruması

            ziyaret.Add(sonraki);
            if (appendToEnd) zincir.AddLast(sonraki); else zincir.AddFirst(sonraki);

            var s = _segments[sonraki];
            // Bir sonraki düğüm: sonraki segmentin, nodeId OLMAYAN ucu.
            nodeId = (s.StartNodeId == nodeId) ? s.EndNodeId : s.StartNodeId;
            gelinenSeg = sonraki;
        }
    }

    // Zincirdeki ilk ve son segmentin dış uçlarını (blok sınır düğümleri) bulur.
    private (Guid start, Guid end) ZincirUclari(List<Guid> segIds)
    {
        if (segIds.Count == 1)
        {
            var s = _segments[segIds[0]];
            return (s.StartNodeId, s.EndNodeId);
        }
        var ilk = _segments[segIds[0]]; var ikinci = _segments[segIds[1]];
        var son = _segments[segIds[^1]]; var sonOnceki = _segments[segIds[^2]];
        // İlk segmentin, ikinciyle paylaşmadığı ucu = blok başlangıcı
        Guid ortakIlk = PaylasilanDugum(ilk, ikinci);
        Guid start = (ilk.StartNodeId == ortakIlk) ? ilk.EndNodeId : ilk.StartNodeId;
        Guid ortakSon = PaylasilanDugum(son, sonOnceki);
        Guid end = (son.StartNodeId == ortakSon) ? son.EndNodeId : son.StartNodeId;
        return (start, end);
    }

    private static Guid PaylasilanDugum(TrackSegment a, TrackSegment b)
    {
        if (a.StartNodeId == b.StartNodeId || a.StartNodeId == b.EndNodeId) return a.StartNodeId;
        return a.EndNodeId;
    }
}
