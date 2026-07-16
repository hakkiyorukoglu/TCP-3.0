using System;
using TrainService.Core.Geometry;

namespace TrainService.Cad;

public class ViewportTransform
{
    public double Scale { get; private set; } = 1.0;
    public Vector2D Pan { get; private set; } = new Vector2D(0, 0);

    public double MinScale { get; set; } = 1e-4;
    public double MaxScale { get; set; } = 1e4;

    public void SetForTest(double scale, Vector2D pan)
    {
        Scale = Math.Clamp(scale, MinScale, MaxScale);
        Pan = pan;
    }

    public void ZoomAt(Vector2D mousePos, double scaleDelta)
    {
        var oldWorld = ScreenToWorld(mousePos);
        Scale = Math.Clamp(Scale * scaleDelta, MinScale, MaxScale);
        Pan = mousePos - oldWorld * Scale;
    }

    public Vector2D ScreenToWorld(Vector2D screenPos)
    {
        return (screenPos - Pan) / Scale;
    }

    public Vector2D WorldToScreen(Vector2D worldPos)
    {
        return worldPos * Scale + Pan;
    }
}
