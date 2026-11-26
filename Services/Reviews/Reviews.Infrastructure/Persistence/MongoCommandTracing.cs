using System.Collections.Concurrent;
using System.Diagnostics;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;

namespace HotelReviews.Infrastructure.Persistence;

internal static class MongoCommandTracing
{
    private static readonly ActivitySource Source = new("HotelReviews.MongoDB");
    private static readonly ConcurrentDictionary<int, Activity> InFlight = new();

    public static void Subscribe(ClusterBuilder clusterBuilder)
    {
        clusterBuilder.Subscribe<CommandStartedEvent>(started =>
        {
            var activity = Source.StartActivity($"mongodb {started.CommandName}", ActivityKind.Client);
            if (activity is null)
            {
                return;
            }

            activity.SetTag("db.system", "mongodb");
            activity.SetTag("db.name", started.DatabaseNamespace.DatabaseName);
            activity.SetTag("db.operation", started.CommandName);
            activity.SetTag("db.collection", GetCollectionName(started));
            activity.SetTag("net.peer.name", started.ConnectionId.ServerId.EndPoint.ToString());

            if (!InFlight.TryAdd(started.RequestId, activity))
            {
                activity.Dispose();
            }
        });

        clusterBuilder.Subscribe<CommandSucceededEvent>(succeeded =>
        {
            if (InFlight.TryRemove(succeeded.RequestId, out var activity))
            {
                activity.SetStatus(ActivityStatusCode.Ok);
                activity.SetTag("db.duration.ms", succeeded.Duration.TotalMilliseconds);
                activity.Dispose();
            }
        });

        clusterBuilder.Subscribe<CommandFailedEvent>(failed =>
        {
            if (InFlight.TryRemove(failed.RequestId, out var activity))
            {
                activity.SetStatus(ActivityStatusCode.Error, failed.Failure.Message);
                activity.SetTag("error.type", failed.Failure.GetType().FullName);
                activity.SetTag("db.duration.ms", failed.Duration.TotalMilliseconds);
                activity.Dispose();
            }
        });
    }

    private static string? GetCollectionName(CommandStartedEvent started)
    {
        var collection = started.Command.GetValue(started.CommandName);
        return collection.IsString ? collection.AsString : null;
    }
}
