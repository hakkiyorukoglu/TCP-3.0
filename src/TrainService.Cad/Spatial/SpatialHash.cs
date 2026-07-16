using System;
using System.Collections.Generic;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Spatial;

/// <summary>Uzamsal hash indeksi. Hücre = CellSize×CellSize mm. O(1) yakınlık sorgusu.
/// CadDocument İÇİNDE yaşar; entity yaşam döngüsüyle senkron tutulur (stale index imkansız).
/// HOT-PATH: Query çağrı başına List TAHSİS ETMEZ — çağıran, yeniden kullanılabilir buffer verir.</summary>
public sealed class SpatialHash
{
    private readonly double _cell;
    private readonly Dictionary<(int cx, int cy), List<Guid>> _cells = new();
    private readonly Dictionary<Guid, BoundingBox> _bounds = new();

    public SpatialHash(double cellSizeMm = 5000) => _cell = cellSizeMm;

    private (int,int) CellOf(double x, double y) => ((int)Math.Floor(x/_cell), (int)Math.Floor(y/_cell));

    public void Add(Guid id, BoundingBox b)
    {
        _bounds[id] = b;
        foreach (var c in CellsCovering(b))
        {
            if (!_cells.TryGetValue(c, out var list)) { list = new(); _cells[c] = list; }
            if (!list.Contains(id)) list.Add(id);
        }
    }
    public void Remove(Guid id)
    {
        if (!_bounds.TryGetValue(id, out var b)) return;
        foreach (var c in CellsCovering(b))
            if (_cells.TryGetValue(c, out var list)) list.Remove(id);
        _bounds.Remove(id);
    }
    public void Update(Guid id, BoundingBox yeni) { Remove(id); Add(id, yeni); }
    public void Clear() { _cells.Clear(); _bounds.Clear(); }

    /// <summary>Bölgeyi kesen entity Id'lerini SONUÇ listesine EKLER (temizlemez — çağıran temizler).
    /// Tahsissiz: çağıran buffer'ı yeniden kullanır.</summary>
    public void Query(BoundingBox box, List<Guid> sonuc)
    {
        var (minx, miny) = CellOf(box.MinX, box.MinY);
        var (maxx, maxy) = CellOf(box.MaxX, box.MaxY);
        for (int cx = minx; cx <= maxx; cx++)
        {
            for (int cy = miny; cy <= maxy; cy++)
            {
                if (_cells.TryGetValue((cx, cy), out var list))
                {
                    for (int i = 0; i < list.Count; i++) sonuc.Add(list[i]);
                }
            }
        }
    }

    private IEnumerable<(int,int)> CellsCovering(BoundingBox b)
    {
        var (minx, miny) = CellOf(b.MinX, b.MinY);
        var (maxx, maxy) = CellOf(b.MaxX, b.MaxY);
        for (int cx = minx; cx <= maxx; cx++)
            for (int cy = miny; cy <= maxy; cy++)
                yield return (cx, cy);
    }
}
