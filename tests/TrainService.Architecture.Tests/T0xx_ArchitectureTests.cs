using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using NetArchTest.Rules;
using System;

namespace TrainService.Architecture.Tests;

public class T0xx_ArchitectureTests
{
    private static System.Reflection.Assembly Core => typeof(TrainService.Core.Geometry.Vector2D).Assembly;
    private static System.Reflection.Assembly Cad  => typeof(TrainService.Cad.CadDocument).Assembly;
    private static System.Reflection.Assembly Data => typeof(TrainService.Data.TrainDbContext).Assembly;
    private static System.Reflection.Assembly Msg  => System.Reflection.Assembly.Load("TrainService.Messaging");
    private static System.Reflection.Assembly Sim  => System.Reflection.Assembly.Load("TrainService.Simulation");

    [Fact]
    public void T001_Core_HicbirProjeyeBagimliDegil()
    {
        var r = Types.InAssembly(Core)
            .ShouldNot().HaveDependencyOnAny(
                "TrainService.Cad", "TrainService.Data", "TrainService.Messaging",
                "TrainService.App", "TrainService.Simulation", "TrainService.Firmware")
            .GetResult();
        r.IsSuccessful.Should().BeTrue(
            $"Core saf olmalı. İhlal edenler: {string.Join(", ", r.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void T002_Cad_SadeceCoreyeBagimli()
    {
        var r = Types.InAssembly(Cad)
            .ShouldNot().HaveDependencyOnAny(
                "TrainService.Data", "TrainService.Messaging", "TrainService.App")
            .GetResult();
        r.IsSuccessful.Should().BeTrue(
            $"Cad yalnızca Core'a bakar. İhlal: {string.Join(", ", r.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void T003_Core_WpfTipiIcermez()
    {
        var r = Types.InAssembly(Core)
            .ShouldNot().HaveDependencyOnAny("System.Windows", "PresentationCore", "PresentationFramework", "WindowsBase")
            .GetResult();
        r.IsSuccessful.Should().BeTrue("Core'a WPF tipi (Point, Brush) sızmamalı");
    }

    [Fact]
    public void T004_Cad_WpfTipiIcermez()
    {
        var r = Types.InAssembly(Cad)
            .ShouldNot().HaveDependencyOnAny("System.Windows", "PresentationCore", "PresentationFramework", "WindowsBase")
            .GetResult();
        r.IsSuccessful.Should().BeTrue(
            $"Cad WPF'siz olmalı. İhlal: {string.Join(", ", r.FailingTypeNames ?? new List<string>())}");
    }

    [Fact]
    public void T005_Messaging_DataBagimsiz()
    {
        var r = Types.InAssembly(Msg)
            .ShouldNot().HaveDependencyOn("TrainService.Data")
            .GetResult();
        r.IsSuccessful.Should().BeTrue("Messaging cihaz kayıtlarını arayüz üzerinden almalı, Data'ya bağlanmamalı");
    }

    [Fact]
    public void T006_Simulation_IskeletBos()
    {
        var tipler = Sim.GetTypes()
            .Where(t => t.IsPublic && !t.IsInterface && !t.IsAbstract && !t.IsEnum)
            .Select(t => t.Name).ToList();
        tipler.Count.Should().BeLessThanOrEqualTo(2,
            $"Simulation erken doldurulmamalı (Roadmap Faz H). Bulunan: {string.Join(", ", tipler)}");
    }

    [Fact]
    public void T007_TumEntityler_CadEntityMirasAlir()
    {
        var baseType = typeof(TrainService.Core.Entities.CadEntity);
        var ihlal = Core.GetTypes()
            .Where(t => t.Namespace == "TrainService.Core.Entities"
                        && t.IsClass && !t.IsAbstract
                        && t != baseType && t.Name != "RouteStep" && t.Name != "EventLog" && t.Name != "Project" && t.Name != "CadLayer"
                        && !baseType.IsAssignableFrom(t))
            .Select(t => t.Name).ToList();
        ihlal.Should().BeEmpty($"CadEntity mirası almayan varlık(lar): {string.Join(", ", ihlal)}");
    }

    [Fact]
    public void T008_TumKomutlar_ICadCommandUygular()
    {
        var iface = typeof(TrainService.Cad.UndoRedo.ICadCommand);
        var ihlal = Cad.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Command")
                        && t != typeof(TrainService.Cad.UndoRedo.CompositeCadCommand)
                        && !iface.IsAssignableFrom(t))
            .Select(t => t.Name).ToList();
        ihlal.Should().BeEmpty($"ICadCommand uygulamayan komut(lar): {string.Join(", ", ihlal)}");
    }
}

