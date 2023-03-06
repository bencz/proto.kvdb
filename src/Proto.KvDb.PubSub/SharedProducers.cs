using System.Collections.Concurrent;
using Proto.Cluster.PubSub;

namespace Proto.KvDb.PubSub;

public static class SharedProducers
{
    private static readonly ConcurrentDictionary<string, Lazy<BatchingProducer>> Producers = new();

    public static BatchingProducer GetProducer(Cluster.Cluster cluster, string topic)
        => GetProducer(cluster, 10000, topic);
    
    public static BatchingProducer GetProducer(Cluster.Cluster cluster, int maxQueueSize, string topic) =>
        Producers.GetOrAdd(
                topic,
                new Lazy<BatchingProducer>(
                    () => new BatchingProducer(cluster.Publisher(), topic, new BatchingProducerConfig
                    {
                        MaxQueueSize = maxQueueSize,
                        OnPublishingError = (_, _, _) => Task.FromResult(PublishingErrorDecision.FailBatchAndContinue)
                    }), true)
            )
            .Value;
}