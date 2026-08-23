namespace TrafficRealtime.Poc.Api.Models
{
    public class RabbitMqChannelSettings
    {
        public string Id { get; set; } = string.Empty;

        public string Exchange { get; set; } = string.Empty;

        public string Queue { get; set; } = string.Empty;

        public string Type { get; set; } = "fanout";

        public bool Durable { get; set; } = true;

        public bool AutoDelete { get; set; } = false;

        public int MessageTtl { get; set; } = 0;

        public RealtimeChannel? Channel { get; set; }
    }
}
