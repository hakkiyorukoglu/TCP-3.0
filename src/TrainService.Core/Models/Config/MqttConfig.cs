namespace TrainService.Core.Models.Config;

public class MqttConfig
{
    public string BrokerIp { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 1883;
}
