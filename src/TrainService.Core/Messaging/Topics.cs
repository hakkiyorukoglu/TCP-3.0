using System;

namespace TrainService.Core.Messaging
{
    public static class Topics
    {
        public const string Commands = "restaurant/commands";
        public static string RfidTelemetry(string id) => $"restaurant/telemetry/{id}/rfid";
        public static string Ack(string id) => $"restaurant/ack/{id}";
        public static string Status(string id) => $"restaurant/status/{id}";
    }

    public record CommandPayload(Guid CmdId, string TrainId, string TargetStationId, string Action);
}
