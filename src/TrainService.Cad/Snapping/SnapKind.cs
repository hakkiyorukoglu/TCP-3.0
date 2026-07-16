namespace TrainService.Cad.Snapping;

/// <summary>Snap türü. Sayısal değerler öncelik DEĞİLDİR (öncelik ISnapProvider.Priority'dedir);
/// aralıklı bırakılmıştır ki ileride ara tür eklemek serileştirmeyi bozmasın.</summary>
public enum SnapKind
{
    None = 0,
    Grid = 10,
    OnSegment = 20,   // v3.0.19 — bugün render switch'inde boş kol olarak var
    Endpoint = 30     // v3.0.19
}
