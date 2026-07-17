using TrainService.Cad.Snapping;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Tools;

public interface ITool
{
    string Name { get; }

    /// <summary>Araç aktifleştiğinde (toolbar'dan seçilince) çağrılır. İç durum sıfırlanır.</summary>
    void Activate(ToolContext ctx);

    /// <summary>Araç bırakılırken çağrılır. Yarım kalan işlem varsa SESSİZCE iptal edilir.</summary>
    void Deactivate(ToolContext ctx);

    void OnPointerMove(SnapResult snapped, ToolContext ctx);
    void OnPointerDown(SnapResult snapped, ToolMouseButton button, ToolContext ctx);
    void OnPointerUp(SnapResult snapped, ToolMouseButton button, ToolContext ctx);
    void OnKeyDown(ToolKey key, ToolContext ctx);

    /// <summary>Görsel katmanın okuyacağı önizleme verisi. null = önizleme yok.</summary>
    PreviewShape? Preview { get; }
}

public abstract record PreviewShape;

public sealed record PreviewLine(Vector2D From, Vector2D To, bool IsValid) : PreviewShape;

public sealed record PreviewRectangle(Vector2D From, Vector2D To, bool IsCrossing) : PreviewShape;

public sealed record PreviewRoute(System.Collections.Generic.IReadOnlyList<TrainService.Core.Entities.RouteStep> Steps, System.Guid AdaySegmentId, bool AdayGecerli) : PreviewShape;
