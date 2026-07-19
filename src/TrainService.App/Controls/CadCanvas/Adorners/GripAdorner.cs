using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace TrainService.App.Controls.CadCanvas.Adorners;

/// <summary>
/// Grip tutmaç tipleri.
/// </summary>
public enum GripType
{
    /// <summary>Köşe/kenar ortası — boyut değiştirme (scale)</summary>
    Stretch,

    /// <summary>Merkez — taşıma (translate)</summary>
    Move,

    /// <summary>Dış halka — döndürme (rotate)</summary>
    Rotate
}

/// <summary>
/// Seçili entity üzerinde görünen grip (tutmaç) Adorner'ı.
/// v3.0.29.20 — Grip Editing.
/// </summary>
public class GripAdorner : Adorner
{
    private readonly List<GripRect> _grips = new();
    private const double GripSize = 8.0;
    private const double RotateRadius = 40.0;

    public GripAdorner(UIElement adornedElement) : base(adornedElement)
    {
        CreateGrips();
    }

    /// <summary>
    /// Entity'nin sınırlarına ve merkezine göre grip dikdörtgenlerini oluşturur.
    /// </summary>
    private void CreateGrips()
    {
        _grips.Clear();
        double w = AdornedElement.DesiredSize.Width;
        double h = AdornedElement.DesiredSize.Height;
        // Eğer entity 0 boyutundaysa (nodes gibi), minimum boyut ata
        if (w < 4) w = 4;
        if (h < 4) h = 4;

        double hs = GripSize / 2;

        // 8 köşe + kenar ortası stretch grip
        _grips.Add(new GripRect(new Rect(w / 2 - hs, -hs, GripSize, GripSize), GripType.Stretch));  // Top-Center
        _grips.Add(new GripRect(new Rect(w / 2 - hs, h - hs, GripSize, GripSize), GripType.Stretch));  // Bottom-Center
        _grips.Add(new GripRect(new Rect(-hs, h / 2 - hs, GripSize, GripSize), GripType.Stretch));  // Left-Center
        _grips.Add(new GripRect(new Rect(w - hs, h / 2 - hs, GripSize, GripSize), GripType.Stretch));  // Right-Center
        _grips.Add(new GripRect(new Rect(-hs, -hs, GripSize, GripSize), GripType.Stretch));  // Top-Left
        _grips.Add(new GripRect(new Rect(w - hs, -hs, GripSize, GripSize), GripType.Stretch));  // Top-Right
        _grips.Add(new GripRect(new Rect(-hs, h - hs, GripSize, GripSize), GripType.Stretch));  // Bottom-Left
        _grips.Add(new GripRect(new Rect(w - hs, h - hs, GripSize, GripSize), GripType.Stretch));  // Bottom-Right

        // 1 merkez move grip
        _grips.Add(new GripRect(new Rect(w / 2 - hs, h / 2 - hs, GripSize, GripSize), GripType.Move));

        // 1 dış halka rotate grip
        _grips.Add(new GripRect(new Rect(w / 2 - hs, -RotateRadius - hs, GripSize, GripSize), GripType.Rotate));
    }

    /// <summary>
    /// Verilen noktada bir grip varsa tipini döndürür, yoksa null.
    /// </summary>
    public GripType? GetGripAt(Point point)
    {
        foreach (var grip in _grips)
        {
            if (grip.Bounds.Contains(point))
                return grip.Type;
        }
        return null;
    }

    /// <summary>
    /// Entity yeniden boyutlandığında grip pozisyonlarını günceller.
    /// </summary>
    public void UpdateGrips(double width, double height)
    {
        CreateGrips();
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        if (_grips.Count == 0) return;

        var stretchBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xAA, 0xFF));  // Mavi kareler
        var stretchPen = new Pen(stretchBrush, 1.5); stretchPen.Freeze();
        stretchBrush.Freeze();

        var moveBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xB9, 0x00));  // Turuncu üçgen
        var movePen = new Pen(moveBrush, 1.5); movePen.Freeze();
        moveBrush.Freeze();

        var rotateBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xCC, 0x66));  // Yeşil daire
        var rotatePen = new Pen(rotateBrush, 1.5); rotatePen.Freeze();
        rotateBrush.Freeze();

        foreach (var grip in _grips)
        {
            switch (grip.Type)
            {
                case GripType.Stretch:
                    dc.DrawRectangle(stretchBrush, stretchPen, grip.Bounds);
                    break;
                case GripType.Move:
                    dc.DrawRectangle(moveBrush, movePen, grip.Bounds);
                    break;
                case GripType.Rotate:
                    dc.DrawEllipse(null, rotatePen, 
                        new Point(grip.Bounds.X + grip.Bounds.Width / 2, 
                                 grip.Bounds.Y + grip.Bounds.Height / 2),
                        grip.Bounds.Width / 2, grip.Bounds.Height / 2);
                    break;
            }
        }
    }

    private readonly struct GripRect
    {
        public Rect Bounds { get; }
        public GripType Type { get; }

        public GripRect(Rect bounds, GripType type)
        {
            Bounds = bounds;
            Type = type;
        }
    }
}