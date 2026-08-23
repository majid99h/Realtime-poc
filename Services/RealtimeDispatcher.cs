using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using TrafficRealtime.Poc.Api.Hubs;
using TrafficRealtime.Poc.Api.Models;

namespace TrafficRealtime.Poc.Api.Services
{
    public sealed class RealtimeDispatcher
      : IRealtimeDispatcher
    {
        private readonly ISubscriptionManager _subscriptions;

        private readonly IHubContext<TrafficHub> _hub;

        private readonly ILogger<RealtimeDispatcher> _logger;


        public RealtimeDispatcher(
            ISubscriptionManager subscriptions,
            IHubContext<TrafficHub> hub,
            ILogger<RealtimeDispatcher> logger)
        {
            _subscriptions = subscriptions;
            _hub = hub;
            _logger = logger;
        }


        public async Task DispatchAsync(
            RealtimeChannel channel,
            TrafficEvent trafficEvent)
        {
            var connectionIds =
                _subscriptions.GetSubscribers(
                    channel,
                    trafficEvent.CameraId);


            if (connectionIds.Count == 0)
            {
                _logger.LogDebug(
                    "No subscribers. " +
                    "Channel={Channel}, Camera={CameraId}",
                    channel,
                    trafficEvent.CameraId);

                return;
            }


            _logger.LogInformation(
                "Dispatching event. " +
                "Channel={Channel}, Camera={CameraId}, " +
                "Subscribers={Subscribers}",
                channel,
                trafficEvent.CameraId,
                connectionIds.Count);


            var tasks =
                connectionIds.Select(
                    connectionId =>
                    {
                        trafficEvent.Channel = channel.ToString();
                        return _hub.Clients
                            .Client(connectionId)
                            .SendAsync(
                                "ReceiveTrafficEvent",
                                trafficEvent);
                    });


            await Task.WhenAll(tasks);
        }
    }
}
