using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TrafficRealtime.Poc.Api.Models;

namespace TrafficRealtime.Poc.Api.Services
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly RealtimeInstanceSettings _instanceSettings;
        private readonly RabbitMqSettings _settings;

        private readonly List<RabbitMqChannelSettings> _channels;

        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<RabbitMqConsumerService> _logger;

        private IConnection? _connection;

        private readonly List<IChannel> _rabbitChannels = new();

        public RabbitMqConsumerService(
            IOptions<RabbitMqSettings> settings,
            IOptions<List<RabbitMqChannelSettings>> channels,
            IOptions<RealtimeInstanceSettings> instanceSettings,
            IServiceProvider serviceProvider,
            ILogger<RabbitMqConsumerService> logger)
        {
            _settings = settings.Value;
            _instanceSettings = instanceSettings.Value;
            _channels = channels.Value;

            _serviceProvider = serviceProvider;

            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.HostName,
                    Port = _settings.Port,
                    UserName = _settings.UserName,
                    Password = _settings.Password
                };

                _logger.LogInformation(
                    "Connecting to RabbitMQ {Host}:{Port}",
                    _settings.HostName,
                    _settings.Port);

                _connection =
                    await factory.CreateConnectionAsync(
                        stoppingToken);

                _logger.LogInformation(
                    "RabbitMQ connection established");

                foreach (var config in _channels)
                {
                    await StartConsumer(
                        config,
                        stoppingToken);
                }

                _logger.LogInformation(
                    "Started {Count} RabbitMQ consumers",
                    _channels.Count);

                await Task.Delay(
                    Timeout.Infinite,
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Application shutting down
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "RabbitMQ consumer service failed");
            }
        }

        private async Task StartConsumer(
            RabbitMqChannelSettings config,
            CancellationToken cancellationToken)
        {
            if (_connection == null)
            {
                throw new InvalidOperationException(
                    "RabbitMQ connection is not available.");
            }

            var channel =
                await _connection.CreateChannelAsync(
                    cancellationToken: cancellationToken);

            _rabbitChannels.Add(channel);

            await channel.ExchangeDeclareAsync(
                exchange: config.Exchange,
                type: config.Type,
                durable: config.Durable,
                autoDelete: config.AutoDelete,
                cancellationToken: cancellationToken);

            var arguments =
                new Dictionary<string, object?>();

            if (config.MessageTtl > 0)
            {
                arguments["x-message-ttl"] =
                    config.MessageTtl;
            }
            var queueName =config.Queue.Replace("{INSTANCE}",_instanceSettings.Name,StringComparison.OrdinalIgnoreCase);
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: config.Durable,
                exclusive: false,
                autoDelete: config.AutoDelete,
                arguments: arguments,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: queueName,
                exchange: config.Exchange,
                routingKey: string.Empty,
                cancellationToken: cancellationToken);

            var consumer =
                new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                await ProcessMessage(
                    config,
                    ea);
            };

            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: true,
                consumer: consumer,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "RabbitMQ consumer started: " +
                "Id={Id}, Exchange={Exchange}, Queue={Queue}, Channel={Channel}",
                config.Id,
                config.Exchange,
                queueName,
                config.Channel);
        }

        private async Task ProcessMessage(
            RabbitMqChannelSettings config,
            BasicDeliverEventArgs ea)
        {
            try
            {
                var json =
                    Encoding.UTF8.GetString(
                        ea.Body.ToArray());

                _logger.LogInformation( "RABBITMQ PAYLOAD | Queue={Queue} | ConfigChannel={Channel} | Payload={Payload}",
                          config.Queue,
                          config.Channel,
                          json);
                var trafficEvent =
                    JsonSerializer.Deserialize<TrafficEvent>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (trafficEvent == null)
                {
                    _logger.LogWarning(
                        "Unable to deserialize RabbitMQ message");
                    return;
                }

                using var scope =
                    _serviceProvider.CreateScope();

                var dispatcher =
                    scope.ServiceProvider
                        .GetRequiredService<IRealtimeDispatcher>();

                if (!RealtimeChannelClassifier.TryClassify(
        trafficEvent,
        config.Channel,
        out var channel))
                {
                    _logger.LogWarning(
                        "Unable to classify RabbitMQ event. " +
                        "EventType={EventType}, Queue={Queue}",
                        trafficEvent.EventType,
                        config.Queue);

                    return;
                }

                _logger.LogInformation(
                    "Classified RabbitMQ event. " +
                    "Queue={Queue}, EventType={EventType}, Channel={Channel}, Camera={CameraId}",
                    config.Queue,
                    trafficEvent.EventType,
                    channel,
                    trafficEvent.CameraId);

                await dispatcher.DispatchAsync(
                    channel,
                    trafficEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing RabbitMQ message. " +
                    "Channel={Channel}",
                    config.Channel);
            }
        }

        public override async Task StopAsync(
            CancellationToken cancellationToken)
        {
            foreach (var channel in _rabbitChannels)
            {
                try
                {
                    await channel.CloseAsync(
                        cancellationToken);
                }
                catch
                {
                    // Ignore shutdown errors
                }
            }

            if (_connection != null)
            {
                try
                {
                    await _connection.CloseAsync(
                        cancellationToken);
                }
                catch
                {
                    // Ignore shutdown errors
                }
            }

            await base.StopAsync(
                cancellationToken);
        }
    }
}
