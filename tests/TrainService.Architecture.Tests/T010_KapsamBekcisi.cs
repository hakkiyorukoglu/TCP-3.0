using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace TrainService.Architecture.Tests;

public class T010_KapsamBekcisi
{
    private static int CountTests(string assemblyName)
    {
        return Assembly.Load(assemblyName)
            .GetTypes()
            .SelectMany(t => t.GetMethods())
            .Sum(m =>
            {
                var attrs = m.GetCustomAttributes();
                if (attrs.Any(a => a.GetType().Name == "FactAttribute")) return 1;
                if (attrs.Any(a => a.GetType().Name == "TheoryAttribute"))
                {
                    int inlineCount = attrs.Count(a => a.GetType().Name == "InlineDataAttribute");
                    return inlineCount > 0 ? inlineCount : 1;
                }
                return 0;
            });
    }

    [Fact]
    public void T010_TestSayisi_TabanAltinaDusemez()
    {
        int cadTests = CountTests("TrainService.Cad.Tests");
        int appTests = CountTests("TrainService.App.Tests");
        int dataTests = CountTests("TrainService.Data.Tests");

        cadTests.Should().BeGreaterThanOrEqualTo(144, "Cad.Tests tabanı 144 (v3.0.28 — T310 FeatureTree testleri)");
        appTests.Should().BeGreaterThanOrEqualTo(10, "App.Tests tabanı 10 (v3.0.29 — T320 RadialMenu testleri)");
        dataTests.Should().BeGreaterThanOrEqualTo(26, "Data.Tests tabanı 26 (v3.0.29 — T560/T561 round-trip testleri)");
    }
}
