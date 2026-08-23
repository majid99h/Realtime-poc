using TrafficRealtime.Poc.Api.Models;

namespace TrafficRealtime.Poc.Api.Services
{
    public interface IRealtimeDispatcher
    {
        Task DispatchAsync(
        RealtimeChannel channel,
        TrafficEvent trafficEvent);
    }
}
