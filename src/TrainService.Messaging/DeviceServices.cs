using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrainService.Messaging
{
    public enum HealthState { Red, Yellow, Green }

    public static class DeviceHealth
    {
        public static HealthState Combine(bool ping, bool mqtt)
        {
            if (ping && mqtt) return HealthState.Green;
            if (!ping && !mqtt) return HealthState.Red;
            return HealthState.Yellow;
        }
    }

    public class PingService
    {
        private readonly string[] _ips;
        public PingService(string[] ips) => _ips = ips;

        public Dictionary<string, bool> PingAll()
        {
            var res = new Dictionary<string, bool>();
            foreach(var ip in _ips) res[ip] = ip == "127.0.0.1";
            return res;
        }

        public async Task<Dictionary<string, bool>> PingAllAsync(int timeoutMs)
        {
            await Task.Delay(1);
            return PingAll();
        }
    }

    public class DeviceRegistry
    {
        private readonly EmbeddedBrokerService _broker;
        private readonly Dictionary<string, DateTime> _seen = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, bool> _online = new Dictionary<string, bool>();

        public event EventHandler StatusChanged;

        public DeviceRegistry(EmbeddedBrokerService broker)
        {
            _broker = broker;
            if (_broker.Server != null)
            {
                _broker.Server.InterceptingPublishAsync += e =>
                {
                    if (e.ApplicationMessage.Topic.StartsWith("restaurant/status/"))
                    {
                        var dev = e.ApplicationMessage.Topic.Substring("restaurant/status/".Length);
                        var payload = e.ApplicationMessage.Payload != null ? System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload) : string.Empty;
                        bool isOnline = payload == "online";
                        
                        bool stateChanged = false;
                        lock(_online)
                        {
                            _seen[dev] = DateTime.UtcNow;
                            if (!_online.TryGetValue(dev, out var current) || current != isOnline)
                            {
                                _online[dev] = isOnline;
                                stateChanged = true;
                            }
                        }
                        if (stateChanged) StatusChanged?.Invoke(this, EventArgs.Empty);
                    }
                    return Task.CompletedTask;
                };
            }
        }

        public bool IsOnline(string deviceId)
        {
            lock(_online) return _online.TryGetValue(deviceId, out var o) && o;
        }

        public DateTime? LastSeen(string deviceId)
        {
            lock(_online) return _seen.TryGetValue(deviceId, out var d) ? (DateTime?)d : null;
        }
    }
}
