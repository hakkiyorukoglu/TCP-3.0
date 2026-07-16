using System;

namespace TrainService.App.Logging
{
    public class LogBus
    {
        private static readonly Lazy<LogBus> _instance = new Lazy<LogBus>(() => new LogBus());
        public static LogBus Instance => _instance.Value;

        private LogBus() { }

        public event EventHandler<EventArgs> OnLog;

        public void Publish(string source, string message)
        {
            OnLog?.Invoke(this, EventArgs.Empty);
        }
    }
}
