using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using TrainService.Cad;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Tests;

public class T3xx_ViewportTests
{
    [Fact]
    public void T301_Transform_RoundTrip_500Nokta()
    {
        var t = new ViewportTransform();
        t.SetForTest(scale: 2.5, pan: new Vector2D(100, -50));
        var rnd = new Random(42);
        for (int i = 0; i < 500; i++)
        {
            var p = new Vector2D(rnd.NextDouble() * 1000, rnd.NextDouble() * 1000);
            var screen = t.WorldToScreen(p);
            var world = t.ScreenToWorld(screen);
            world.X.Should().BeApproximately(p.X, 1e-6);
            world.Y.Should().BeApproximately(p.Y, 1e-6);
        }
    }

    [Fact]
    public void T302_ZoomAt_ImlecAltiSabit()
    {
        var t = new ViewportTransform();
        t.SetForTest(scale: 1.0, pan: new Vector2D(0, 0));
        
        var mouseScreen = new Vector2D(300, 200);
        var oldWorld = t.ScreenToWorld(mouseScreen);
        
        t.ZoomAt(mouseScreen, 1.5);
        
        var newWorld = t.ScreenToWorld(mouseScreen);
        newWorld.X.Should().BeApproximately(oldWorld.X, 1e-6);
        newWorld.Y.Should().BeApproximately(oldWorld.Y, 1e-6);
    }

    [Fact]
    public void T303_ZoomAt_20Kez_BirikimliHataYok()
    {
        var t = new ViewportTransform();
        var mouseScreen = new Vector2D(500, 500);
        var startWorld = t.ScreenToWorld(mouseScreen);

        for (int i = 0; i < 10; i++) t.ZoomAt(mouseScreen, 1.25);
        for (int i = 0; i < 10; i++) t.ZoomAt(mouseScreen, 0.8);

        var endWorld = t.ScreenToWorld(mouseScreen);
        endWorld.X.Should().BeApproximately(startWorld.X, 1e-4);
        endWorld.Y.Should().BeApproximately(startWorld.Y, 1e-4);
    }

    [Fact]
    public void T304_Scale_SinirKontrol()
    {
        var t = new ViewportTransform();
        t.SetForTest(scale: 1.0, pan: default);
        for (int i = 0; i < 200; i++) t.ZoomAt(new Vector2D(100, 100), 2.0);  // aşırı zoom-in
        t.Scale.Should().BeLessThanOrEqualTo(t.MaxScale, "üst sınır aşılmamalı");
        for (int i = 0; i < 400; i++) t.ZoomAt(new Vector2D(100, 100), 0.5);  // aşırı zoom-out
        t.Scale.Should().BeGreaterThanOrEqualTo(t.MinScale, "alt sınır aşılmamalı");
    }
}
