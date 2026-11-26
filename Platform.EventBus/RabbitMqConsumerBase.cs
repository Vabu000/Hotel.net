using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus;

public abstract class RabbitMqConsumerBase(
    IConnection connection,
    ILogger logger,
    string queueName,
    int maxRetries = 3) : BackgroundService
{
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await EventTopology.DeclareAsync(_channel, stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer, stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    protected abstract Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken);

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        using var activity = TracePropagation.StartConsumerActivity(ea, queueName);

        try
        {
            await ProcessMessageAsync(ea, CancellationToken.None);
            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Malformed message {MessageId} on {Queue}, routing to DLQ",
                ea.BasicProperties.MessageId, queueName);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            await RetryOrDeadLetterAsync(ea, ex);
        }
    }

    private async Task RetryOrDeadLetterAsync(BasicDeliverEventArgs ea, Exception ex)
    {
        var headers = ea.BasicProperties.Headers ?? new Dictionary<string, object?>();
        var attempt = headers.TryGetValue("x-retry-count", out var raw) ? Convert.ToInt32(raw) : 0;

        if (attempt >= maxRetries)
        {
            logger.LogError(ex, "Message {MessageId} on {Queue} failed after {Attempts} attempts, sending to DLQ",
                ea.BasicProperties.MessageId, queueName, attempt);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        var nextHeaders = new Dictionary<string, object?>(headers)
        {
            ["x-retry-count"] = attempt + 1
        };

        var retryProps = new BasicProperties(ea.BasicProperties)
        {
            Headers = nextHeaders
        };

        await _channel!.BasicPublishAsync(ea.Exchange, ea.RoutingKey, mandatory: false, retryProps, ea.Body);
        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

        logger.LogWarning(ex, "Message {MessageId} on {Queue} scheduled for retry {Attempt}/{MaxRetries}",
            ea.BasicProperties.MessageId, queueName, attempt + 1, maxRetries);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
