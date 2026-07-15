using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TrainService.Core.Abstractions;
using TrainService.Messaging.Health;
using Xunit;

namespace TrainService.Messaging.Tests
{
    public class PingServiceTests
    {
        [Fact]
        public async Task PingService_Should_Fire_Event_For_Localhost()
        {
            // Arrange
            var mockLogBus = new Mock<ILogBus>();
            var mockDeviceRegistry = new Mock<IDeviceRegistry>();
            
            var pingService = new PingService(mockLogBus.Object, mockDeviceRegistry.Object);
            pingService.SetIpList(new List<string> { "127.0.0.1" });
            
            bool eventFired = false;
            bool? pingResult = null;
            string? pingedIp = null;
            
            var tcs = new TaskCompletionSource<bool>();
            
            pingService.OnPingStatusChanged += (ip, status) => 
            {
                eventFired = true;
                pingResult = status;
                pingedIp = ip;
                tcs.TrySetResult(true);
            };
            
            // Act
            pingService.StartPinging();
            
            // Wait for max 3 seconds for the ping loop to trigger the event
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(3000));
            
            pingService.StopPinging();
            pingService.Dispose();
            
            // Assert
            Assert.True(eventFired, "Ping status event did not fire.");
            Assert.Equal("127.0.0.1", pingedIp);
            Assert.True(pingResult, "Localhost ping should be successful.");
        }
    }
}
