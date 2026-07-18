using System;
using System.Collections.Generic;
using System.Linq;
using TrainService.Cad.FeatureTree;
using TrainService.Core.Entities;

namespace TrainService.Cad;

// CadDocument'a Feature Tree oluşturma yeteneği ekleyen partial class.
public partial class CadDocument
{
    /// <summary>
    /// Mevcut doküman içeriğinden hiyerarşik Feature Tree yapısını üretir.
    /// 5 grup döner: Katmanlar, Raylar, Hatlar, Makaslar, Rampalar.
    /// </summary>
    public List<FeatureTreeItem> BuildFeatureTree()
    {
        var roots = new List<FeatureTreeItem>();

        // 1. Katmanlar grubu
        var katmanGroup = new FeatureTreeItem
        {
            Name = "Katmanlar",
            EntityType = "Group",
            Icon = "\U0001F5C2" // 🗂
        };
        foreach (var layer in Layers.OrderBy(l => l.DisplayOrder))
        {
            katmanGroup.Children.Add(new FeatureTreeItem
            {
                Name = layer.Name,
                EntityId = layer.Id,
                EntityType = "Layer",
                Icon = layer.IsVisible ? "\U0001F441" : "\U0001F648", // 👁 / 🙈
                IsVisible = layer.IsVisible,
                IsLocked = layer.IsLocked
            });
        }
        roots.Add(katmanGroup);

        // 2. Raylar grubu (katmana göre gruplu)
        var rayGroup = new FeatureTreeItem
        {
            Name = "Raylar",
            EntityType = "Group",
            Icon = "\u26CC" // ⛌
        };
        foreach (var segment in Entities.OfType<TrackSegment>())
        {
            var layerName = GetLayerName(segment.LayerId);
            rayGroup.Children.Add(new FeatureTreeItem
            {
                Name = $"Segment {segment.Id.ToString()[..8]} [{layerName}]",
                EntityId = segment.Id,
                EntityType = "TrackSegment",
                Icon = "\u2014" // —
            });
        }
        roots.Add(rayGroup);

        // 3. Hatlar grubu
        var hatGroup = new FeatureTreeItem
        {
            Name = "Hatlar",
            EntityType = "Group",
            Icon = "\u27A1" // ➡
        };
        foreach (var route in Entities.OfType<Route>())
        {
            hatGroup.Children.Add(new FeatureTreeItem
            {
                Name = $"Route {route.Id.ToString()[..8]} ({route.Steps.Count} ad\u0131m)",
                EntityId = route.Id,
                EntityType = "Route",
                Icon = "\u27A1" // ➡
            });
        }
        roots.Add(hatGroup);

        // 4. Makaslar grubu
        var makasGroup = new FeatureTreeItem
        {
            Name = "Makaslar",
            EntityType = "Group",
            Icon = "\u2B21" // ⬡
        };
        foreach (var sw in Entities.OfType<RailSwitch>())
        {
            makasGroup.Children.Add(new FeatureTreeItem
            {
                Name = $"Makas ({sw.Position.X:F0}, {sw.Position.Y:F0})",
                EntityId = sw.Id,
                EntityType = "RailSwitch",
                Icon = "\u2B21" // ⬡
            });
        }
        roots.Add(makasGroup);

        // 5. Rampalar grubu
        var rampaGroup = new FeatureTreeItem
        {
            Name = "Rampalar",
            EntityType = "Group",
            Icon = "\u25B2" // ▲
        };
        foreach (var ramp in Entities.OfType<Ramp>())
        {
            rampaGroup.Children.Add(new FeatureTreeItem
            {
                Name = $"Rampa ({ramp.Position.X:F0}, {ramp.Position.Y:F0})",
                EntityId = ramp.Id,
                EntityType = "Ramp",
                Icon = "\u25B2" // ▲
            });
        }
        roots.Add(rampaGroup);

        return roots;
    }

    private string GetLayerName(Guid layerId)
    {
        return TryGetLayer(layerId, out var layer) ? layer.Name : "?";
    }
}
