using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TrainService.App.Controls.RadialMenu;

/// <summary>
/// Radial (pie) context menu control.
/// Draws slices on a Canvas inside a Popup, supports hover highlight and click-to-execute.
/// </summary>
public partial class RadialMenuControl : UserControl
{
    private const double MenuRadius = 120;
    private const double CenterX = 140;
    private const double CenterY = 140;
    private const double InnerRadius = 30;

    private IReadOnlyList<RadialMenuItem> _items = Array.Empty<RadialMenuItem>();
    private int _hoveredIndex = -1;
    private readonly Brush _defaultFill = new SolidColorBrush(Color.FromArgb(200, 60, 60, 60));
    private readonly Brush _hoverFill = new SolidColorBrush(Color.FromArgb(220, 80, 80, 80));
    private readonly Brush _strokeBrush = new SolidColorBrush(Color.FromArgb(180, 180, 180, 180));
    private readonly Brush _disabledFill = new SolidColorBrush(Color.FromArgb(100, 40, 40, 40));
    private readonly Brush _textBrush = new SolidColorBrush(Colors.White);
    private readonly Brush _disabledTextBrush = new SolidColorBrush(Color.FromArgb(120, 200, 200, 200));
    private readonly Pen _strokePen;

    public RadialMenuControl()
    {
        InitializeComponent();
        _strokePen = new Pen(_strokeBrush, 1.5);
    }

    /// <summary>
    /// Opens the radial menu at the given screen position with the specified items.
    /// </summary>
    public void ShowAt(Point screenPosition, IReadOnlyList<RadialMenuItem> items)
    {
        _items = items ?? Array.Empty<RadialMenuItem>();
        _hoveredIndex = -1;
        BuildSlices();
        MenuPopup.PlacementRectangle = new Rect(screenPosition.X - CenterX, screenPosition.Y - CenterY, 0, 0);
        MenuPopup.IsOpen = true;
    }

    /// <summary>
    /// Closes the radial menu.
    /// </summary>
    public void Close()
    {
        MenuPopup.IsOpen = false;
    }

    private void BuildSlices()
    {
        MenuCanvas.Children.Clear();

        int count = _items.Count;
        if (count == 0) return;

        double sweepAngle = 360.0 / count;
        double startAngle = -90.0; // start from top

        for (int i = 0; i < count; i++)
        {
            var item = _items[i];
            double endAngle = startAngle + sweepAngle;

            // --- Slice background (Path) ---
            var slice = CreateSlicePath(startAngle, endAngle, item.IsEnabled ? _defaultFill : _disabledFill);
            slice.Tag = i;
            MenuCanvas.Children.Add(slice);

            // --- Label text ---
            double midAngle = (startAngle + endAngle) / 2.0;
            double midRad = midAngle * Math.PI / 180.0;
            double labelRadius = InnerRadius + (MenuRadius - InnerRadius) * 0.6;
            double lx = CenterX + labelRadius * Math.Cos(midRad);
            double ly = CenterY + labelRadius * Math.Sin(midRad);

            var label = new TextBlock
            {
                Text = item.Label,
                Foreground = item.IsEnabled ? _textBrush : _disabledTextBrush,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(label, lx - 20);
            Canvas.SetTop(label, ly - 8);
            label.Width = 40;
            label.TextAlignment = TextAlignment.Center;
            MenuCanvas.Children.Add(label);

            // --- Icon glyph ---
            double iconRadius = InnerRadius + (MenuRadius - InnerRadius) * 0.25;
            double ix = CenterX + iconRadius * Math.Cos(midRad);
            double iy = CenterY + iconRadius * Math.Sin(midRad);

            var icon = new TextBlock
            {
                Text = item.IconGlyph,
                Foreground = item.IsEnabled ? _textBrush : _disabledTextBrush,
                FontSize = 16,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(icon, ix - 10);
            Canvas.SetTop(icon, iy - 10);
            icon.Width = 20;
            icon.TextAlignment = TextAlignment.Center;
            MenuCanvas.Children.Add(icon);

            startAngle = endAngle;
        }

        // --- Center circle ---
        var centerEllipse = new Ellipse
        {
            Width = InnerRadius * 2,
            Height = InnerRadius * 2,
            Fill = new SolidColorBrush(Color.FromArgb(180, 30, 30, 30)),
            Stroke = _strokeBrush,
            StrokeThickness = 1.5
        };
        Canvas.SetLeft(centerEllipse, CenterX - InnerRadius);
        Canvas.SetTop(centerEllipse, CenterY - InnerRadius);
        MenuCanvas.Children.Add(centerEllipse);
    }

    private Path CreateSlicePath(double startAngleDeg, double endAngleDeg, Brush fill)
    {
        double startRad = startAngleDeg * Math.PI / 180.0;
        double endRad = endAngleDeg * Math.PI / 180.0;

        Point pOuterStart = new Point(
            CenterX + MenuRadius * Math.Cos(startRad),
            CenterY + MenuRadius * Math.Sin(startRad));
        Point pOuterEnd = new Point(
            CenterX + MenuRadius * Math.Cos(endRad),
            CenterY + MenuRadius * Math.Sin(endRad));
        Point pInnerStart = new Point(
            CenterX + InnerRadius * Math.Cos(startRad),
            CenterY + InnerRadius * Math.Sin(startRad));
        Point pInnerEnd = new Point(
            CenterX + InnerRadius * Math.Cos(endRad),
            CenterY + InnerRadius * Math.Sin(endRad));

        bool largeArc = (endAngleDeg - startAngleDeg) > 180.0;

        var segments = new PathSegmentCollection
        {
            new ArcSegment(pOuterEnd, new Size(MenuRadius, MenuRadius), 0, largeArc, SweepDirection.Clockwise, true),
            new LineSegment(pInnerEnd, true),
            new ArcSegment(pInnerStart, new Size(InnerRadius, InnerRadius), 0, largeArc, SweepDirection.Counterclockwise, true),
            new LineSegment(pOuterStart, true)
        };

        var figure = new PathFigure(pOuterStart, segments, true);
        var geometry = new PathGeometry(new[] { figure });

        return new Path
        {
            Data = geometry,
            Fill = fill,
            Stroke = _strokeBrush,
            StrokeThickness = 1.5
        };
    }

    private int HitTestSlice(Point canvasPos)
    {
        double dx = canvasPos.X - CenterX;
        double dy = canvasPos.Y - CenterY;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        if (dist < InnerRadius || dist > MenuRadius)
            return -1;

        double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        if (angle < -90) angle += 360;

        // Normalize to 0-360 with 0 at top
        double normalized = (angle + 90) % 360;
        if (normalized < 0) normalized += 360;

        int count = _items.Count;
        if (count == 0) return -1;

        double sweepAngle = 360.0 / count;
        int index = (int)(normalized / sweepAngle);
        return (index >= 0 && index < count) ? index : -1;
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(MenuCanvas);
        int hit = HitTestSlice(pos);
        if (hit != _hoveredIndex)
        {
            _hoveredIndex = hit;
            UpdateHover();
        }
    }

    private void OnCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(MenuCanvas);
        int hit = HitTestSlice(pos);
        if (hit >= 0 && hit < _items.Count && _items[hit].IsEnabled)
        {
            var cmd = _items[hit].Command;
            MenuPopup.IsOpen = false;
            cmd?.Invoke();
        }
        else
        {
            MenuPopup.IsOpen = false;
        }
    }

    private void OnCanvasMouseLeave(object sender, MouseEventArgs e)
    {
        _hoveredIndex = -1;
        UpdateHover();
    }

    private void OnPopupClosed(object sender, EventArgs e)
    {
        _hoveredIndex = -1;
    }

    private void UpdateHover()
    {
        for (int i = 0; i < MenuCanvas.Children.Count; i++)
        {
            if (MenuCanvas.Children[i] is Path slice && slice.Tag is int idx)
            {
                bool enabled = idx < _items.Count && _items[idx].IsEnabled;
                slice.Fill = (idx == _hoveredIndex && enabled)
                    ? _hoverFill
                    : (enabled ? _defaultFill : _disabledFill);
            }
        }
    }
}
