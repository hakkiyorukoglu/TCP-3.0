namespace TrainService.Cad.Snapping;

/// <summary>Snap turu. Sayisal degerler oncelik DEGILDIR (oncelik ISnapProvider.Priority'dedir);
/// aralikli birakilmistir ki ileride ara tur eklemek serilestirmeyi bozmasin.</summary>
public enum SnapKind
{
    None = 0,
    Grid = 10,
    OnSegment = 20,
    Endpoint = 30,
    Midpoint = 40     // v3.0.29.22 — segment orta noktasi
}