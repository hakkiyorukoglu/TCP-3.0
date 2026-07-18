using System;

namespace TrainService.App.Controls.RadialMenu;

/// <summary>
/// Represents a single item in the radial (context) menu.
/// Immutable record — lightweight, no WPF dependencies beyond Action.
/// </summary>
public sealed record RadialMenuItem(
    string Label,
    string IconGlyph,
    Action Command,
    bool IsEnabled = true
);
