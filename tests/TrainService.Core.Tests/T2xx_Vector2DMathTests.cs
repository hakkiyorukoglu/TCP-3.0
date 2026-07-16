using FluentAssertions;
using TrainService.Core.Geometry;
using Xunit;

namespace TrainService.Core.Tests;

public class T2xx_Vector2DMathTests
{
    [Fact]
    public void T210_DistanceSquaredToSegment_DikIzdusum()
    {
        // (5,5) noktasının (0,0)-(10,0) segmentine izdüşümü (5,0), mesafe kare 25
        var d = Vector2DMath.DistanceSquaredToSegment(new(5,5), new(0,0), new(10,0), out var proj);
        d.Should().BeApproximately(25, 1e-9);
        proj.X.Should().BeApproximately(5, 1e-9);
        proj.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void T211_DistanceSquaredToSegment_UcNoktaClamp()
    {
        // (-5,0) segmentin SOLUNDA → en yakın nokta (0,0) (uzantıya değil uca clamp)
        var d = Vector2DMath.DistanceSquaredToSegment(new(-5,0), new(0,0), new(10,0), out var proj);
        proj.X.Should().BeApproximately(0, 1e-9);
        d.Should().BeApproximately(25, 1e-9);
    }

    [Fact]
    public void T212_DistanceSquaredToSegment_DejenereSegment_NaNYok()
    {
        // ★ a==b (sıfır uzunluk) → a döner, bölme hatası YOK
        var d = Vector2DMath.DistanceSquaredToSegment(new(3,4), new(1,1), new(1,1), out var proj);
        proj.Should().Be(new Vector2D(1,1));
        double.IsNaN(d).Should().BeFalse();
        d.Should().BeApproximately(13, 1e-9);   // (3-1)²+(4-1)² = 4+9 = 13
    }

    [Fact]
    public void T213_DistanceSquaredToSegment_TamUstunde_Sifir()
    {
        var d = Vector2DMath.DistanceSquaredToSegment(new(5,0), new(0,0), new(10,0), out _);
        d.Should().BeApproximately(0, 1e-9);
    }
}
