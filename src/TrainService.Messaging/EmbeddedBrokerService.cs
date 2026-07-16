using MQTTnet;
using MQTTnet.Server;
using System.Threading.Tasks;

namespace TrainService.Messaging
{
    public class EmbeddedBrokerService
    {
        private readonly int _port;
        private readonly int _keepAlive;
        private MqttServer _mqttServer;

        public EmbeddedBrokerService(int port, int keepAliveSec)
        {
            _port = port;
            _keepAlive = keepAliveSec;
        }
        
        public MqttServer Server => _mqttServer;

        public async Task StartAsync()
        {
            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(_port);
            _mqttServer = new MqttFactory().CreateMqttServer(optionsBuilder.Build());
            await _mqttServer.StartAsync();
        }

        public async Task StopAsync()
        {
            if (_mqttServer != null)
            {
                await _mqttServer.StopAsync();
                _mqttServer.Dispose();
                _mqttServer = null;
            }
        }
    }
}
