using System;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using TrainService.App;
using TrainService.App.Logging;
using TrainService.Cad;

namespace TrainService.App.Tests
{
    public class T7xx_AppTests
    {
        [Fact]
        public void T701_LogBus_SingletonOlmali()
        {
            var bus1 = LogBus.Instance;
            var bus2 = LogBus.Instance;
            Assert.NotNull(bus1);
            Assert.Same(bus1, bus2);
        }

        [Fact]
        public void T702_LogBus_EventFirlatir()
        {
            bool tetiklendi = false;
            LogBus.Instance.OnLog += (_, _) => tetiklendi = true;
            LogBus.Instance.Publish("test", "test");
            Assert.True(tetiklendi);
        }

        [Fact]
        public void T801_AppHost_Olusturulabilir()
        {
            var host = App.CreateHostForTest();
            Assert.NotNull(host);
            
            var proj = host.Services.GetService<ICadProject>();
            Assert.NotNull(proj);
        }
    }
}


