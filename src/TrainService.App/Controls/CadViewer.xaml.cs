using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TrainService.Core.Entities;

namespace TrainService.App.Controls;

public partial class CadViewer : UserControl
{
    private Point _lastMousePosition;
    private bool _isDragging;

    public CadViewer()
    {
        InitializeComponent();
    }

    public void RenderMap(List<TrackNode> nodes, List<TrackSegment> segments)
    {
        MapCanvas.Children.Clear();

        foreach (var seg in segments)
        {
            var startNode = nodes.Find(n => n.Id == seg.StartNodeId);
            var endNode = nodes.Find(n => n.Id == seg.EndNodeId);
            if (startNode != null && endNode != null)
            {
                var line = new Line
                {
                    X1 = startNode.Position.X,
                    Y1 = startNode.Position.Y,
                    X2 = endNode.Position.X,
                    Y2 = endNode.Position.Y,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 4,
                    ToolTip = $"Segment: {seg.Id}"
                };
                MapCanvas.Children.Add(line);
            }
        }

        foreach (var node in nodes)
        {
            var ellipse = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = Brushes.DodgerBlue,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                ToolTip = $"Node: {node.Id}\nX:{node.Position.X} Y:{node.Position.Y}"
            };
            Canvas.SetLeft(ellipse, node.Position.X - 8);
            Canvas.SetTop(ellipse, node.Position.Y - 8);
            MapCanvas.Children.Add(ellipse);
        }
    }

    private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
        MapScale.ScaleX *= zoomFactor;
        MapScale.ScaleY *= zoomFactor;
    }

    private void Grid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _lastMousePosition = e.GetPosition(this);
        _isDragging = true;
        this.CaptureMouse();
    }

    private void Grid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        this.ReleaseMouseCapture();
    }

    private void Grid_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var currentPos = e.GetPosition(this);
            var dx = currentPos.X - _lastMousePosition.X;
            var dy = currentPos.Y - _lastMousePosition.Y;
            
            MapTranslate.X += dx / MapScale.ScaleX;
            MapTranslate.Y += dy / MapScale.ScaleY;

            _lastMousePosition = currentPos;
        }
    }
}
