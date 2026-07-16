using System;
using FluentAssertions;
using Xunit;
using TrainService.Cad;

namespace TrainService.Cad.Tests;

public class CadDocumentTests
{
    [Fact]
    public void T1712_CadDocument_GridSize_FiresEvent_OnlyOnRealChange()
    {
        var doc = new CadDocument();
        int fireCount = 0;
        doc.Changed += (s, e) => 
        {
            if (e.Kind == DocumentChangeKind.GridChanged)
                fireCount++;
        };

        doc.GridSizeMm = 50.0;
        fireCount.Should().Be(1);

        // Same value should not fire again
        doc.GridSizeMm = 50.0;
        fireCount.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void T1715_CadDocument_GridSize_InvalidThrows(double invalidSize)
    {
        var doc = new CadDocument();
        Action act = () => doc.GridSizeMm = invalidSize;
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
