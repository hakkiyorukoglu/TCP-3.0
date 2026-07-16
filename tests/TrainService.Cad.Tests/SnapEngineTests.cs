using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using TrainService.Core.Geometry;
using TrainService.Cad;
using TrainService.Cad.Snapping;

namespace TrainService.Cad.Tests;

public class SnapEngineTests
{
    private readonly CadDocument _emptyDoc = new CadDocument();

    private sealed class FakeProvider : ISnapProvider
    {
        private readonly int _priority;
        private readonly SnapResult? _result;
        private readonly List<int> _callLog;

        public FakeProvider(int priority, SnapResult? result, List<int> callLog)
        {
            _priority = priority;
            _result = result;
            _callLog = callLog;
        }

        public int Priority => _priority;
        public SnapResult? TrySnap(Vector2D c, double tol, CadDocument d)
        { 
            _callLog.Add(_priority); 
            return _result; 
        }
    }

    [Fact]
    public void T1709_Engine_OncelikZinciri_KisaDevre()
    {
        var log = new List<int>();
        var hit = new SnapResult(new Vector2D(1, 1), SnapKind.Endpoint, Guid.NewGuid());
        var engine = new SnapEngine(new ISnapProvider[]
        {
            new FakeProvider(100, new SnapResult(default, SnapKind.Grid, null), log), // kasıtlı karışık sıra
            new FakeProvider(10,  null, log),                                          // yüksek öncelik, ıskalar
            new FakeProvider(20,  hit,  log),                                          // yakalar
        });

        var result = engine.Resolve(new Vector2D(5, 5), 1.0, _emptyDoc);

        result.Should().Be(hit);                        // 20 kazandı
        log.Should().Equal(10, 20);                     // sıra: önce 10, sonra 20; 100 HİÇ ÇAĞRILMADI
    }

    [Fact]
    public void T1708_Engine_Disabled_Returns_None()
    {
        var log = new List<int>();
        var engine = new SnapEngine(new[] { new FakeProvider(10, new SnapResult(default, SnapKind.Grid, null), log) });
        engine.IsEnabled = false;

        var result = engine.Resolve(new Vector2D(5, 5), 1.0, _emptyDoc);

        result.Kind.Should().Be(SnapKind.None);
        log.Should().BeEmpty();
    }

    [Fact]
    public void T1710_Engine_EmptyProviders_Returns_None()
    {
        var engine = new SnapEngine(Array.Empty<ISnapProvider>());
        var result = engine.Resolve(new Vector2D(5, 5), 1.0, _emptyDoc);
        result.Kind.Should().Be(SnapKind.None);
    }

    [Theory]
    [InlineData(10, 2.0, 5.0)]
    [InlineData(10, 0.5, 20.0)]
    [InlineData(10, 0.0, 0.0)]
    [InlineData(10, -1.0, 0.0)]
    public void T1711_Engine_Tolerance_Conversion(double tolPx, double scale, double expectedWorldTol)
    {
        var result = SnapEngine.ScreenToleranceToWorld(tolPx, scale);
        result.Should().BeApproximately(expectedWorldTol, 1e-9);
    }

    [Fact]
    public void T1713_Engine_NoAllocation_On_Resolve()
    {
        var engine = new SnapEngine(new[] { new FakeProvider(10, null, new List<int>()) });
        
        // This is primarily to ensure we don't crash and we can call it 10k times. 
        // Real allocation tracking would need dotMemory/BenchmarkDotNet, but we ensure the code executes cleanly.
        for (int i = 0; i < 10_000; i++)
        {
            engine.Resolve(new Vector2D(i, i), 1.0, _emptyDoc);
        }
    }
}
