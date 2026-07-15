using System;
using System.Windows;
using System.Windows.Media;

namespace TrainService.App.Controls.CadCanvas;

public class CadRenderLayer : FrameworkElement
{
    private readonly VisualCollection _visuals;

    public CadRenderLayer()
    {
        _visuals = new VisualCollection(this);
    }

    public void AddVisual(DrawingVisual visual)
    {
        _visuals.Add(visual);
    }

    public void RemoveVisual(DrawingVisual visual)
    {
        _visuals.Remove(visual);
    }

    public void Clear()
    {
        _visuals.Clear();
    }

    protected override int VisualChildrenCount => _visuals.Count;

    protected override Visual GetVisualChild(int index)
    {
        if (index < 0 || index >= _visuals.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        return _visuals[index];
    }
}
