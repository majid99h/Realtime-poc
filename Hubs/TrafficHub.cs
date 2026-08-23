using Microsoft.AspNetCore.SignalR;
using TrafficRealtime.Poc.Api.Models;
using TrafficRealtime.Poc.Api.Services;

namespace TrafficRealtime.Poc.Api.Hubs;

public class TrafficHub : Hub
{
    private readonly ISubscriptionManager _subscriptions;

    private readonly ILogger<TrafficHub> _logger;


    public TrafficHub(
        ISubscriptionManager subscriptions,
        ILogger<TrafficHub> logger)
    {
        _subscriptions = subscriptions;
        _logger = logger;
    }


    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "Connected: {ConnectionId}",
            Context.ConnectionId);

        await base.OnConnectedAsync();
    }


    public override async Task OnDisconnectedAsync(
        Exception? exception)
    {
        _subscriptions.RemoveConnection(
            Context.ConnectionId);


        _logger.LogInformation(
            "Disconnected: {ConnectionId}",
            Context.ConnectionId);


        await base.OnDisconnectedAsync(
            exception);
    }


    public Task SubscribeChannel(
        string channel,
        IEnumerable<int> cameraIds)
    {
        if (!Enum.TryParse<RealtimeChannel>(
                channel,
                true,
                out var realtimeChannel))
        {
            throw new HubException(
                $"Unknown channel: {channel}");
        }


        _subscriptions.Subscribe(
            Context.ConnectionId,
            realtimeChannel,
            cameraIds);


        _logger.LogInformation(
            "Connection {ConnectionId} subscribed " +
            "to {Channel} with {CameraCount} cameras",
            Context.ConnectionId,
            realtimeChannel,
            cameraIds.Distinct().Count());


        return Task.CompletedTask;
    }


    public Task UpdateChannel(
        string channel,
        IEnumerable<int> cameraIds)
    {
        if (!Enum.TryParse<RealtimeChannel>(
                channel,
                true,
                out var realtimeChannel))
        {
            throw new HubException(
                $"Unknown channel: {channel}");
        }


        _subscriptions.UpdateSubscription(
            Context.ConnectionId,
            realtimeChannel,
            cameraIds);


        return Task.CompletedTask;
    }


    public Task UnsubscribeChannel(
        string channel)
    {
        if (!Enum.TryParse<RealtimeChannel>(
                channel,
                true,
                out var realtimeChannel))
        {
            throw new HubException(
                $"Unknown channel: {channel}");
        }


        _subscriptions.Unsubscribe(
            Context.ConnectionId,
            realtimeChannel);


        return Task.CompletedTask;
    }


    public Task UnsubscribeAll()
    {
        _subscriptions.RemoveConnection(
            Context.ConnectionId);

        return Task.CompletedTask;
    }
}