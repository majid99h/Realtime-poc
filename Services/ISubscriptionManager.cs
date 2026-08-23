using TrafficRealtime.Poc.Api.Models;

namespace TrafficRealtime.Poc.Api.Services
{
    public interface ISubscriptionManager
    {
        void Subscribe(
            string connectionId,
            RealtimeChannel channel,
            IEnumerable<int> cameraIds);

        void UpdateSubscription(
            string connectionId,
            RealtimeChannel channel,
            IEnumerable<int> cameraIds);

        void Unsubscribe(
            string connectionId,
            RealtimeChannel channel);

        void RemoveConnection(
            string connectionId);

        IReadOnlyCollection<string> GetSubscribers(
            RealtimeChannel channel,
            int cameraId);

        int GetCameraSubscriberCount(
            RealtimeChannel channel,
            int cameraId);

        int ConnectionCount { get; }

        int SubscriptionCount { get; }

        void Clear();
    }
}
