using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace TrainService.Architecture.Tests;

public class T011_TrivialTestGuard
{
    [Fact]
    public void T011_TrivialAssertYasak()
    {
        // Tüm test derlemelerinde gövdesi çok kısa veya sahte test avı.
        var testAsmlari = new[]
        {
            typeof(T011_TrivialTestGuard).Assembly,
            Assembly.Load("TrainService.Core.Tests"),  Assembly.Load("TrainService.Cad.Tests"),
            Assembly.Load("TrainService.Data.Tests"),  Assembly.Load("TrainService.Messaging.Tests"),
            Assembly.Load("TrainService.App.Tests"),   Assembly.Load("TrainService.Simulation.Tests"),
        };
        var supheliler = new List<string>();
        foreach (var asm in testAsmlari)
            foreach (var t in asm.GetTypes())
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    bool testMi = m.GetCustomAttributes().Any(a => a is FactAttribute or TheoryAttribute);
                    if (!testMi) continue;
                    var il = m.GetMethodBody()?.GetILAsByteArray();
                    if (il is null || il.Length <= 12)                       // ldc.i4.1 + call + ret ≈ 12 byte altı
                        supheliler.Add($"{t.FullName}.{m.Name} (IL={il?.Length ?? 0}B)");
                }
        supheliler.Should().BeEmpty($"içi boş/sahte testler: {string.Join("; ", supheliler)}");
    }
}
