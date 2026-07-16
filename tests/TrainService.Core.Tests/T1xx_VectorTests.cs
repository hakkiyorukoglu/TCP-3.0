using FluentAssertions;
using TrainService.Core.Geometry;
using Xunit;

namespace TrainService.Core.Tests;

public class T1xx_VectorTests
{
    [Fact]
    public void T101_Vector2D_Matematigi()
    {
        var v = new Vector2D(3, 4);
        v.Length.Should().Be(5);
    }

    [Fact]
    public void T102_Vector2D_Normalize_ZeroVector_ReturnsZero()
    {
        var v = new Vector2D(0, 0);
        var n = v.Normalized();
        n.X.Should().Be(0);
        n.Y.Should().Be(0);
    }

    [Fact]
    public void T103_Vector2D_Length_ZeroVector()
    {
        var v = new Vector2D(0, 0);
        v.Length.Should().Be(0);
    }

    [Fact]
    public void T105_Vector2D_PerpendicularCW()
    {
        var v = new Vector2D(1, 0);
        var p = v.PerpendicularCW();
        p.X.Should().Be(0);
        p.Y.Should().Be(-1);
    }
}
