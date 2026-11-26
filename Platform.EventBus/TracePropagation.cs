using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus;

public static class TracePropagation
{
    public static readonly ActivitySource Source = new("Platform.Messaging");
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public static void Inject(BasicProperties properties)
    {
        if (Activity.Current is not { } activity)
        {
            return;
        }

        properties.Headers ??= new Dictionary<string, object?>();
        Propagator.Inject(
            new PropagationContext(activity.Context, Baggage.Current),
            properties.Headers,
            static (headers, key, value) => headers[key] = value);
    }

    public static Activity? StartConsumerActivity(BasicDeliverEventArgs ea, string queueName)
    {
        var parentContext = Propagator.Extract(
            default,
            ea.BasicProperties.Headers,
            static (headers, key) =>
            {
                if (headers is not null && headers.TryGetValue(key, out var raw) && raw is byte[] bytes)
                {
                    return [Encoding.UTF8.GetString(bytes)];
                }

                return [];
            });

        var activity = Source.StartActivity($"{queueName} process", ActivityKind.Consumer, parentContext.ActivityContext);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", queueName);
        activity?.SetTag("messaging.message.id", ea.BasicProperties.MessageId);
        return activity;
    }
}
