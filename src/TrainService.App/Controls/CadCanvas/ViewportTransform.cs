using System;
using System.Windows;
using TrainService.Core.Geometry;

namespace TrainService.App.Controls.CadCanvas;

public sealed class ViewportTransform
{
    private double _scale = 1.0;
    
    // Ekranda (piksel cinsinden) kaç mm gösterildiğini belirtir
    public double Scale
    {
        get => _scale;
        set => _scale = Math.Max(0.001, Math.Min(1000.0, value)); // limit zoom
    }

    // Sol üst köşenin Dünya Koordinatındaki mm değeri
    public Vector2D PanOffset { get; set; } = new Vector2D(0, 0);

    public Point WorldToScreen(Vector2D w)
    {
        return new Point((w.X - PanOffset.X) * Scale, (w.Y - PanOffset.Y) * Scale);
    }

    public Vector2D ScreenToWorld(Point s)
    {
        return new Vector2D(s.X / Scale + PanOffset.X, s.Y / Scale + PanOffset.Y);
    }

    public void ZoomAt(Point screenPoint, double factor)
    {
        var worldBeforeZoom = ScreenToWorld(screenPoint);
        Scale *= factor;
        
        // Yeniden hesaplanan ölçeğe göre pan ofsetini ayarla ki fare altındaki dünya noktası sabit kalsın
        PanOffset = new Vector2D(
            worldBeforeZoom.X - screenPoint.X / Scale,
            worldBeforeZoom.Y - screenPoint.Y / Scale
        );
    }
}
