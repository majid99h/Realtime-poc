using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TrafficRealtime.Poc.Api.Models;
using TrafficRealtime.Poc.Api.Services;

namespace TrafficRealtime.Poc.Api.Controllers
{
    [ApiController]
    [Route("api/benchmark")]
    public class BenchmarkController : ControllerBase
    {
        private readonly ISubscriptionManager _subscriptionManager;

        public BenchmarkController(
            ISubscriptionManager subscriptionManager)
        {
            _subscriptionManager = subscriptionManager;
        }

        [HttpPost("subscriptions")]
        public IActionResult CreateSubscriptions(
            int users = 100,
            int camerasPerUser = 1000)
        {
            _subscriptionManager.Clear();

            var stopwatch = Stopwatch.StartNew();

            var cameraIds = Enumerable
                .Range(1, camerasPerUser)
                .ToArray();

            for (int user = 1; user <= users; user++)
            {
                var connectionId =
                    $"benchmark-{user}";

                _subscriptionManager.Subscribe(
                    connectionId,
                    RealtimeChannel.Transaction,
                    cameraIds);
            }

            stopwatch.Stop();

            return Ok(new
            {
                users,
                camerasPerUser,

                channel = RealtimeChannel.Transaction,

                totalSubscriptions =
                _subscriptionManager.SubscriptionCount,

                connectionCount =
                _subscriptionManager.ConnectionCount,

                elapsedMilliseconds =
                stopwatch.ElapsedMilliseconds,

                camera1Subscribers =
                _subscriptionManager
                    .GetCameraSubscriberCount(
                        RealtimeChannel.Transaction,
                        1),

                camera500Subscribers =
                _subscriptionManager
                    .GetCameraSubscriberCount(
                        RealtimeChannel.Transaction,
                        500),

                camera1000Subscribers =
                _subscriptionManager
                    .GetCameraSubscriberCount(
                        RealtimeChannel.Transaction,
                        1000)
           
        });
        }

        [HttpGet("subscriptions")]
        public IActionResult GetStatistics()
        {
            return Ok(new
            {
                connections =
                _subscriptionManager.ConnectionCount,

                subscriptions =
                _subscriptionManager.SubscriptionCount,

                transactionCamera1Subscribers =
                _subscriptionManager
                    .GetCameraSubscriberCount(
                        RealtimeChannel.Transaction,
                        1),

                transactionCamera500Subscribers =
                _subscriptionManager
                    .GetCameraSubscriberCount(
                        RealtimeChannel.Transaction,
                        500),

                transactionCamera1000Subscribers =
                _subscriptionManager
                    .GetCameraSubscriberCount(
                        RealtimeChannel.Transaction,
                        1000)
            });
        }

        [HttpDelete("subscriptions")]
        public IActionResult Clear()
        {
            _subscriptionManager.Clear();

            return Ok(new
            {
                message = "Subscriptions cleared"
            });
        }
    }
}
