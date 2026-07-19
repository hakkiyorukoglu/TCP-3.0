using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TrainService.Cad;
using TrainService.Cad.Selection;
using TrainService.Core.Entities;

namespace TrainService.App.Controls.PropertiesPanel;

/// <summary>
/// Seçili entity'nin özelliklerini gösteren sağ kenar paneli.
/// SelectionService ile senkronize çalışır.
/// </summary>
public class PropertiesPanelControl : UserControl
{
    private SelectionService? _selection;
    private CadDocument? _document;
    private Guid? _currentEntityId;

    private readonly StackPanel _contentPanel;
    private readonly TextBlock _lblNoSelection;
    private readonly StackPanel _panelContent;

    // Property accessors (test edilebilirlik için public)
    public bool IsEmpty => _currentEntityId == null;
    public string SelectedEntityType { get; private set; } = string.Empty;
    public Guid EntityId => _currentEntityId ?? Guid.Empty;
    public double PositionX { get; private set; }
    public double PositionY { get; private set; }

    public PropertiesPanelControl()
    {
        _contentPanel = new StackPanel();

        // Başlık
        _contentPanel.Children.Add(new TextBlock
        {
            Text = "ÖZELLİKLER",
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // "Seçili entity yok" mesajı
        _lblNoSelection = new TextBlock
        {
            Text = "Seçili entity yok",
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
            FontStyle = FontStyles.Italic,
            Visibility = Visibility.Visible
        };
        _contentPanel.Children.Add(_lblNoSelection);

        // Panel içeriği (seçim olunca görünür)
        _panelContent = new StackPanel { Visibility = Visibility.Collapsed };
        _contentPanel.Children.Add(_panelContent);

        // Stil
        this.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
        this.BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
        this.BorderThickness = new Thickness(1, 0, 0, 0);
        this.Padding = new Thickness(8);
        this.Content = _contentPanel;
    }

    /// <summary>
    /// SelectionService'e bağlanır ve seçim değişikliklerini dinler.
    /// </summary>
    public void AttachSelection(SelectionService? sel, CadDocument? doc)
    {
        // Önceki bağlantıyı temizle
        if (_selection != null)
            _selection.SelectionChanged -= OnSelectionChanged;

        _selection = sel;
        _document = doc;

        if (sel != null)
        {
            sel.SelectionChanged += OnSelectionChanged;
            RefreshPanel(sel, doc);
        }
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        RefreshPanel(_selection, _document);
    }

    private void RefreshPanel(SelectionService? sel, CadDocument? doc)
    {
        _panelContent.Children.Clear();
        
        if (sel == null || doc == null || sel.SelectedIds.Count == 0)
        {
            _currentEntityId = null;
            SelectedEntityType = string.Empty;
            PositionX = 0;
            PositionY = 0;
            _lblNoSelection.Visibility = Visibility.Visible;
            _panelContent.Visibility = Visibility.Collapsed;
            return;
        }

        var entityId = sel.SelectedIds.First();
        if (!doc.TryGetEntity(entityId, out var entity))
        {
            _currentEntityId = null;
            SelectedEntityType = string.Empty;
            _lblNoSelection.Visibility = Visibility.Visible;
            _panelContent.Visibility = Visibility.Collapsed;
            return;
        }

        _currentEntityId = entityId;
        SelectedEntityType = entity.GetType().Name;
        _lblNoSelection.Visibility = Visibility.Collapsed;
        _panelContent.Visibility = Visibility.Visible;

        var labelStyle = new Style(typeof(TextBlock));
        labelStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty,
            new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88))));
        labelStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 11.0));
        labelStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(0, 8, 0, 2)));

        var valueStyle = new Style(typeof(TextBox));
        valueStyle.Setters.Add(new Setter(TextBox.BackgroundProperty,
            new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x25))));
        valueStyle.Setters.Add(new Setter(TextBox.ForegroundProperty,
            new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC))));
        valueStyle.Setters.Add(new Setter(TextBox.IsReadOnlyProperty, true));
        valueStyle.Setters.Add(new Setter(TextBox.BorderBrushProperty,
            new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44))));
        valueStyle.Setters.Add(new Setter(TextBox.FontFamilyProperty,
            new System.Windows.Media.FontFamily("Consolas")));
        valueStyle.Setters.Add(new Setter(TextBox.FontSizeProperty, 11.0));

        // ID
        AddField("ID", entity.Id.ToString(), labelStyle, valueStyle);

        // Layer
        var layer = doc.Layers.FirstOrDefault(l => l.Id == entity.LayerId);
        AddField("Katman", layer?.Name ?? entity.LayerId.ToString(), labelStyle, valueStyle);

        // Tür
        AddField("Tür", SelectedEntityType, labelStyle, valueStyle);

        // Tip-spesifik alanlar
        switch (entity)
        {
            case TrackNode node:
                PositionX = node.Position.X;
                PositionY = node.Position.Y;
                AddField("X", $"{node.Position.X:F2} mm", labelStyle, valueStyle);
                AddField("Y", $"{node.Position.Y:F2} mm", labelStyle, valueStyle);
                AddField("Z", $"{node.Z:F2} mm", labelStyle, valueStyle);
                AddField("Role", node.Role.ToString(), labelStyle, valueStyle);
                break;

            case TrackSegment seg:
                if (doc.TryGetEntity(seg.StartNodeId, out var sn) && sn is TrackNode sNode &&
                    doc.TryGetEntity(seg.EndNodeId, out var en) && en is TrackNode eNode)
                {
                    PositionX = sNode.Position.X;
                    PositionY = sNode.Position.Y;
                    AddField("Start Node", seg.StartNodeId.ToString(), labelStyle, valueStyle);
                    AddField("End Node", seg.EndNodeId.ToString(), labelStyle, valueStyle);
                    AddField("Uzunluk", $"{seg.LengthMm:F2} mm", labelStyle, valueStyle);
                }
                break;

            case Route route:
                PositionX = 0;
                PositionY = 0;
                AddField("Adım Sayısı", route.Steps.Count.ToString(), labelStyle, valueStyle);
                if (route.Steps.Count > 0)
                {
                    var firstStep = route.Steps[0];
                    if (doc.TryGetEntity(firstStep.SegmentId, out var rse) && rse is TrackSegment rseg &&
                        doc.TryGetEntity(rseg.StartNodeId, out var rsn) && rsn is TrackNode rsNode)
                    {
                        AddField("Başlangıç", $"{rsNode.Position.X:F2}, {rsNode.Position.Y:F2} mm", labelStyle, valueStyle);
                    }
                }
                break;

            case RailSwitch sw:
                PositionX = sw.Position.X;
                PositionY = sw.Position.Y;
                AddField("X", $"{sw.Position.X:F2} mm", labelStyle, valueStyle);
                AddField("Y", $"{sw.Position.Y:F2} mm", labelStyle, valueStyle);
                AddField("Rotation", $"{sw.RotationDeg:F1}°", labelStyle, valueStyle);
                AddField("State", sw.State.ToString(), labelStyle, valueStyle);
                break;

            case Ramp ramp:
                PositionX = ramp.Position.X;
                PositionY = ramp.Position.Y;
                AddField("X", $"{ramp.Position.X:F2} mm", labelStyle, valueStyle);
                AddField("Y", $"{ramp.Position.Y:F2} mm", labelStyle, valueStyle);
                AddField("Start Z", $"{ramp.StartZ:F2} mm", labelStyle, valueStyle);
                AddField("End Z", $"{ramp.EndZ:F2} mm", labelStyle, valueStyle);
                break;
        }
    }

    private void AddField(string label, string value, Style labelStyle, Style valueStyle)
    {
        var lbl = new TextBlock { Text = label, Style = labelStyle };
        var txt = new TextBox { Text = value, Style = valueStyle };
        _panelContent.Children.Add(lbl);
        _panelContent.Children.Add(txt);
    }

    /// <summary>
    /// Testler için: panele eklenen alanlardan değer okuma.
    /// </summary>
    public string? GetPropertyValue(string propertyName)
    {
        for (int i = 0; i < _panelContent.Children.Count - 1; i++)
        {
            if (_panelContent.Children[i] is TextBlock lbl &&
                lbl.Text == propertyName &&
                _panelContent.Children[i + 1] is TextBox txt)
            {
                return txt.Text;
            }
        }
        return null;
    }
}