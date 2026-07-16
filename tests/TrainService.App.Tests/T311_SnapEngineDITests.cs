using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TrainService.App;
using TrainService.Cad.Snapping;
using Xunit;

namespace TrainService.App.Tests;

public class T311_SnapEngineDITests
{
    [Fact]
    public void T311_AppXaml_DI_RegisterCheck()
    {
        var host = App.CreateHostForTest();
        var providers = host.Services.GetServices<ISnapProvider>().ToList();
        
        providers.Should().Contain(p => p.GetType() == typeof(EndpointSnapProvider));
        providers.Should().Contain(p => p.GetType() == typeof(OnSegmentSnapProvider));
        providers.Should().Contain(p => p.GetType() == typeof(GridSnapProvider));
    }
}
