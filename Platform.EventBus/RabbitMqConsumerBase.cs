using System.Globalization;
using System.Diagnostics;
using System.Text;
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
    int maxAttempts = 3) : BackgroundService
{
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await EventTopology.DeclareAsync(_channel, stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (sender, ea) => OnMessageReceivedAsync(sender, ea, stoppingToken);

        await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer, stoppingToken);
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown cancels the consumer loop; unacknowledged messages are requeued on channel close.
        }
    }

    protected abstract Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken);

    private async Task OnMessageReceivedAsync(
        object sender,
        BasicDeliverEventArgs ea,
        CancellationToken stoppingToken)
    {
        using var activity = TracePropagation.StartConsumerActivity(ea, queueName);

        try
        {
            await ProcessMessageAsync(ea, stoppingToken);
            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
        catch (JsonException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Malformed message {MessageId} on {Queue}, routing to DLQ",
                ea.BasicProperties.MessageId, queueName);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            await RetryOrDeadLetterAsync(ea, ex, stoppingToken);
        }
    }

    private async Task RetryOrDeadLetterAsync(
        BasicDeliverEventArgs ea,
        Exception ex,
        CancellationToken cancellationToken)
    {
        var headers = ea.BasicProperties.Headers ?? new Dictionary<string, object?>();
        int attempt;
        try
        {
            attempt = ReadAttempt(headers);
        }
        catch (JsonException headerException)
        {
            logger.LogError(
                headerException,
                "Message {MessageId} on {Queue} has an invalid retry header, routing to DLQ",
                ea.BasicProperties.MessageId,
                queueName);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken);
            return;
        }

        if (attempt + 1 >= maxAttempts)
        {
            logger.LogError(ex, "Message {MessageId} on {Queue} failed after {Attempts} attempts, sending to DLQ",
                ea.BasicProperties.MessageId, queueName, attempt + 1);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken);
            return;
        }

        var nextAttempt = attempt + 1;
        var nextHeaders = new Dictionary<string, object?>(headers)
        {
            ["x-retry-count"] = nextAttempt
        };

        var retryProps = new BasicProperties(ea.BasicProperties)
        {
            Headers = nextHeaders
        };

        await Task.Delay(TimeSpan.FromMilliseconds(250 * nextAttempt), cancellationToken);
        await _channel!.BasicPublishAsync(
            ea.Exchange,
            ea.RoutingKey,
            mandatory: false,
            retryProps,
            ea.Body,
            cancellationToken);
        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);

        logger.LogWarning(ex, "Message {MessageId} on {Queue} scheduled for retry {Retry}/{MaxRetries}",
            ea.BasicProperties.MessageId, queueName, nextAttempt, maxAttempts - 1);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null)
        {
            if (_channel.IsOpen)
            {
                await _channel.CloseAsync(cancellationToken);
            }
            await _channel.DisposeAsync();
        }
    }

    private static int ReadAttempt(IDictionary<string, object?> headers)
    {
        if (!headers.TryGetValue("x-retry-count", out var raw) || raw is null)
        {
            return 0;
        }

        var value = raw switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.Span),
            _ => Convert.ToString(raw, CultureInfo.InvariantCulture)
        };

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var attempt) || attempt < 0)
        {
            throw new JsonException("Invalid x-retry-count message header");
        }

        return attempt;
    }
}
