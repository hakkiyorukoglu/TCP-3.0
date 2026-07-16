using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;
using TrainService.Core.Enums;

namespace TrainService.Core.Tests;

public class T2xx_EntityTests
{
    [Fact]
    public void T201_CadEntity_Id_BenzersizVeBosDegil()
    {
        var idler = Enumerable.Range(0,1000)
            .Select(_ => new TrackNode{ Position = default, LayerId = Guid.NewGuid() }.Id).ToList();
        idler.Should().OnlyHaveUniqueItems();
        idler.Should().NotContain(Guid.Empty);
    }

    [Fact]
    public void T202_TrackSegment_LengthMm_OkunurYazilir()
    {
        var s = new TrackSegment { LengthMm = 5000 };
        s.LengthMm.Should().Be(5000);
    }

    [Fact]
    public void T203_Ramp_GradePercent_Hesabi()
    {
        new Ramp{ StartZ=0, EndZ=400, LengthMm=8000 }.GradePercent.Should().BeApproximately(5.0, 1e-9);
    }

    [Fact]
    public void T204_Ramp_GradePercent_SifirUzunluk_Patlamaz()
    {
        var r = new Ramp{ StartZ=0, EndZ=400, LengthMm=0 };
        var act = () => r.GradePercent;
        act.Should().NotThrow();
        double.IsNaN(r.GradePercent).Should().BeFalse();
        double.IsInfinity(r.GradePercent).Should().BeFalse();
        r.GradePercent.Should().Be(0);
    }

    [Fact]
    public void T205_RouteStep_Direction_RoundTrip()
    {
        var step = new RouteStep(Guid.NewGuid(), TravelDirection.Forward);
        step.Direction.Should().Be(TravelDirection.Forward);
    }

    [Fact]
    public void T206_Entity_Bounds_NegatifKoordinat()
    {
        var n = new TrackNode{ Position = new Vector2D(-100, -50) };
        n.Bounds.Should().NotBeNull();   // Bounds hesabı negatif koordinatta çökmemeli
    }
}
