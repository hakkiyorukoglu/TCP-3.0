using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TrainService.Core.Abstractions;
using TrainService.Core.Messaging.Contracts;
using Xunit;
using TrainService.Messaging.Commands; // Düzeltildi

namespace TrainService.Core.Tests;

public class DispatchServiceTests
{
    [Fact]
    public async Task SendCommandAndWaitAckAsync_ShouldReturnFalse_WhenNoAckReceived()
    {
        // Arrange
        var mockHub = new Mock<IMqttHub>();
        var mockLogBus = new Mock<ILogBus>();

        var dispatchService = new DispatchService(mockHub.Object, mockLogBus.Object);
        dispatchService.StartListening();

        var cmd = new CommandDto(Guid.NewGuid().ToString(), "Train-1", "Station-1", "ROUTE_TO_STATION");

        // Act & Assert (Timeout durumunda TaskCanceledException yakalanıp false dönüyor)
        var result = await dispatchService.SendCommandAndWaitAckAsync(cmd, 100);
        Assert.False(result);
    }

    [Fact]
    public async Task SendCommandAndWaitAckAsync_ShouldSucceed_WhenAckReceived()
    {
        // Arrange
        var mockHub = new Mock<IMqttHub>();
        var mockLogBus = new Mock<ILogBus>();

        var dispatchService = new DispatchService(mockHub.Object, mockLogBus.Object);
        dispatchService.StartListening();

        var cmdId = Guid.NewGuid().ToString();
        var cmd = new CommandDto(cmdId, "Train-1", "Station-1", "ROUTE_TO_STATION");

        // We simulate that exactly 50ms after sending, a mock ACK arrives
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            
            // Invoke the event from the mock MqttHub as if an ACK came in
            var ackPayload = $"{{\"cmdId\":\"{cmdId}\",\"divert\":true}}";
            mockHub.Raise(m => m.OnMessageReceived += null, $"restaurant/ack/Station-1", ackPayload);
        });

        // Act
        // This will block until the simulated ACK arrives (which is in 50ms)
        var result = await dispatchService.SendCommandAndWaitAckAsync(cmd, 2000);

        // Assert
        Assert.True(result);
    }
}
