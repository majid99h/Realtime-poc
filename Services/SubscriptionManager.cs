using System.Collections.Concurrent;
using TrafficRealtime.Poc.Api.Models;

namespace TrafficRealtime.Poc.Api.Services
{
    public sealed class SubscriptionManager : ISubscriptionManager
    {
        /*
         * Channel + Camera
         *
         * Transaction + Camera 3
         *          ↓
         *     Connection IDs
         *
         * This is the primary lookup index.
         */
        private readonly ConcurrentDictionary<
            SubscriptionKey,
            ConcurrentDictionary<string, byte>>
            _cameraIndex = new();

        /*
         * Connection
         *     ↓
         * Channel
         *     ↓
         * Camera IDs
         *
         * This is used when updating/removing subscriptions.
         */
        private readonly ConcurrentDictionary<
            string,
            ConcurrentDictionary<RealtimeChannel, HashSet<int>>>
            _connectionSubscriptions = new();

        private readonly object _sync = new();


        public void Subscribe(
            string connectionId,
            RealtimeChannel channel,
            IEnumerable<int> cameraIds)
        {
            UpdateSubscription(
                connectionId,
                channel,
                cameraIds);
        }


        public void UpdateSubscription(
            string connectionId,
            RealtimeChannel channel,
            IEnumerable<int> cameraIds)
        {
            var newCameraIds =
                cameraIds
                    .Distinct()
                    .ToHashSet();

            lock (_sync)
            {
                /*
                 * Remove the old subscription for this
                 * connection/channel first.
                 */
                RemoveChannelSubscription(
                    connectionId,
                    channel);


                var subscriptions =
                    _connectionSubscriptions.GetOrAdd(
                        connectionId,
                        _ => new ConcurrentDictionary<
                            RealtimeChannel,
                            HashSet<int>>());


                subscriptions[channel] =
                    newCameraIds;


                /*
                 * Add the connection to the direct
                 * Channel + Camera index.
                 */
                foreach (var cameraId in newCameraIds)
                {
                    var key =
                        new SubscriptionKey(
                            channel,
                            cameraId);


                    var connections =
                        _cameraIndex.GetOrAdd(
                            key,
                            _ => new ConcurrentDictionary<
                                string,
                                byte>());


                    connections.TryAdd(
                        connectionId,
                        0);
                }
            }
        }


        public void Unsubscribe(
            string connectionId,
            RealtimeChannel channel)
        {
            lock (_sync)
            {
                RemoveChannelSubscription(
                    connectionId,
                    channel);
            }
        }


        public void RemoveConnection(
            string connectionId)
        {
            lock (_sync)
            {
                if (!_connectionSubscriptions.TryRemove(
                        connectionId,
                        out var subscriptions))
                {
                    return;
                }


                foreach (var subscription in subscriptions)
                {
                    var channel =
                        subscription.Key;

                    var cameraIds =
                        subscription.Value;


                    foreach (var cameraId in cameraIds)
                    {
                        RemoveFromCameraIndex(
                            connectionId,
                            channel,
                            cameraId);
                    }
                }
            }
        }


        public IReadOnlyCollection<string> GetSubscribers(
            RealtimeChannel channel,
            int cameraId)
        {
            var key =
                new SubscriptionKey(
                    channel,
                    cameraId);


            if (!_cameraIndex.TryGetValue(
                    key,
                    out var connections))
            {
                return Array.Empty<string>();
            }


            return connections.Keys.ToArray();
        }


        public int GetCameraSubscriberCount(
            RealtimeChannel channel,
            int cameraId)
        {
            var key =
                new SubscriptionKey(
                    channel,
                    cameraId);


            if (!_cameraIndex.TryGetValue(
                    key,
                    out var connections))
            {
                return 0;
            }


            return connections.Count;
        }


        public int ConnectionCount =>
        _connectionSubscriptions.Count;

        public int SubscriptionCount =>
            _cameraIndex.Values.Sum(x => x.Count);

        private void RemoveChannelSubscription(
            string connectionId,
            RealtimeChannel channel)
        {
            if (!_connectionSubscriptions.TryGetValue(
                    connectionId,
                    out var subscriptions))
            {
                return;
            }


            if (!subscriptions.TryRemove(
                    channel,
                    out var cameraIds))
            {
                return;
            }


            foreach (var cameraId in cameraIds)
            {
                RemoveFromCameraIndex(
                    connectionId,
                    channel,
                    cameraId);
            }


            if (subscriptions.IsEmpty)
            {
                _connectionSubscriptions.TryRemove(
                    connectionId,
                    out _);
            }
        }


        private void RemoveFromCameraIndex(
            string connectionId,
            RealtimeChannel channel,
            int cameraId)
        {
            var key =
                new SubscriptionKey(
                    channel,
                    cameraId);


            if (!_cameraIndex.TryGetValue(
                    key,
                    out var connections))
            {
                return;
            }


            connections.TryRemove(
                connectionId,
                out _);


            if (connections.IsEmpty)
            {
                _cameraIndex.TryRemove(
                    key,
                    out _);
            }
        }
        public void Clear()
        {
            lock (_sync)
            {
                _cameraIndex.Clear();
                _connectionSubscriptions.Clear();
            }
        }

        private readonly record struct SubscriptionKey(
            RealtimeChannel Channel,
            int CameraId);
    }
}
