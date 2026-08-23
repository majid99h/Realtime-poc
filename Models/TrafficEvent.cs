namespace TrafficRealtime.Poc.Api.Models
{
    public class TrafficEvent
    {
        public int CameraId { get; set; }

        public string Channel { get; set; } = "";

        public string EventType { get; set; } = "";

        public string Message { get; set; } = "";

        public DateTime Timestamp { get; set; }
    }
}
