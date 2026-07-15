using System;
using System.Threading.Tasks;

namespace TrainService.Core.Abstractions;

public interface IMqttHub
{
    Task ConnectAsync();
    Task DisconnectAsync();
    Task PublishAsync(string topic, string payload);
    Task SubscribeAsync(string topic);
    event Action<string, string>? OnMessageReceived;
}
