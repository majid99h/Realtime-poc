using TrafficRealtime.Poc.Api.Models;

namespace TrafficRealtime.Poc.Api.Services
{
    public static class RealtimeChannelClassifier
    {
        public static bool TryClassify(
            TrafficEvent trafficEvent,
            RealtimeChannel? configuredChannel,
            out RealtimeChannel channel)
        {
            // If the RabbitMQ source has a fixed channel
            // such as alert -> Alarm, use it.
            if (configuredChannel.HasValue)
            {
                channel = configuredChannel.Value;
                return true;
            }

            // Dynamic channels such as live/watchlist
            // are determined from the event itself.
            if (Enum.TryParse<RealtimeChannel>(
                trafficEvent.EventType,
                true,
                out channel))
            {
                return true;
            }

            channel = default;

            return false;
        }
    }
}
