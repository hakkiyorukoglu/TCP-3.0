using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace TrainService.Architecture.Tests;

public class T010_KapsamBekcisi
{
    [Fact]
    public void T010_TestSayisi_TabanAltinaDusemez()
    {
        int cadTests = Assembly.Load("TrainService.Cad.Tests")
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

        cadTests.Should().BeGreaterThanOrEqualTo(134, "Cad.Tests tabanı 134'e çıkarıldı (v3.0.27 — T280 SwitchTool testleri eklendi)");
    }
}
