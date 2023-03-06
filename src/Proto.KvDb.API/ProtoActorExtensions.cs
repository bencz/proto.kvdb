using Google.Protobuf.WellKnownTypes;
using Proto.Cluster;
using Proto.Cluster.Cache;
using Proto.Cluster.Kubernetes;
using Proto.Cluster.Partition;
using Proto.Cluster.PubSub;
using Proto.Cluster.Testing;
using Proto.DependencyInjection;
using Proto.KvDb.PubSub;
using Proto.OpenTelemetry;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Proto.Utils;
using StackExchange.Redis;

namespace Proto.KvDb.API;

public static class ProtoActorExtensions
{
    public static void AddActorSystem(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(serviceProvider =>
        {
            const string clusterName = "Proto.kvdb";
            
            var actorSystemConfig = ActorSystemConfig
                .Setup()
                .WithMetrics()
                .WithDeadLetterThrottleCount(3)
                .WithDeadLetterThrottleInterval(TimeSpan.FromSeconds(1));

            if (builder.Configuration.GetValue<bool>("ProtoActor:DeveloperLogging"))
            {
                actorSystemConfig = actorSystemConfig
                    .WithDeveloperSupervisionLogging(true)
                    .WithDeadLetterRequestLogging(true)
                    .WithDeveloperThreadPoolStatsLogging(true);
            }
            
            var system = new ActorSystem(actorSystemConfig);

            IKeyValueStore<Subscribers> kvStore =
                builder.Configuration["ProtoActor:PubSub:SubscribersStorageType"] == "Redis"
                    ? GetRedisSubscribersStore(builder.Configuration)
                    : new InMemoryKeyValueStore();
            
            var (remoteConfig, clusterProvider) = ConfigureClustering(builder.Configuration);
            
            system
                .WithServiceProvider(serviceProvider)
                .WithRemote(remoteConfig)
                .WithCluster(ClusterConfig
                    .Setup(clusterName, clusterProvider, new PartitionIdentityLookup())
                    .WithClusterKind(TopicActor.Kind, Props.FromProducer(() => new TopicActor(kvStore)))
                    .ConfigureGrainProps(serviceProvider)
                )
                .Cluster()
                .WithPidCacheInvalidation();

            return system;
        });

        builder.Services.AddSingleton(provider => provider.GetRequiredService<ActorSystem>().Cluster());
    }
    
    private static RedisKeyValueStore GetRedisSubscribersStore(IConfiguration config)
    {
        var multiplexer = ConnectionMultiplexer.Connect(config["ProtoActor:PubSub:RedisConnectionString"]);
        var db = multiplexer.GetDatabase();
        return new RedisKeyValueStore(db, config.GetValue<int>("ProtoActor:PubSub:RedisMaxConcurrency"));
    }

    private static ClusterConfig ConfigureGrainProps(this ClusterConfig clusterConfig, IServiceProvider serviceProvider)
    {
        return clusterConfig;
    }

    private static (GrpcNetRemoteConfig, IClusterProvider) ConfigureClustering(IConfiguration config)
        => config["ProtoActor:ClusterProvider"] == "Kubernetes"
            ? ConfigureForKubernetes(config)
            : ConfigureForLocalhost();

    private static (GrpcNetRemoteConfig, IClusterProvider) ConfigureForKubernetes(IConfiguration config)
    {
        var clusterProvider = new KubernetesProvider();

        var remoteConfig = GrpcNetRemoteConfig
            .BindToAllInterfaces(
                advertisedHost: config["ProtoActor:AdvertisedHost"],
                port: 51515)
            .ConfigProtoMessagesMessages()
            .WithLogLevelForDeserializationErrors(LogLevel.Critical)
            .WithRemoteDiagnostics(true); // required by proto.actor dashboard

        return (remoteConfig, clusterProvider);
    }

    private static (GrpcNetRemoteConfig, IClusterProvider) ConfigureForLocalhost()
        => (GrpcNetRemoteConfig
                .BindToLocalhost()
                .WithRemoteDiagnostics(true), // required by proto.actor dashboard
            new TestProvider(new TestProviderOptions(), new InMemAgent()));

    private static GrpcNetRemoteConfig ConfigProtoMessagesMessages(this GrpcNetRemoteConfig grpcNetRemoteConfig)
        => grpcNetRemoteConfig
            .WithProtoMessages(EmptyReflection.Descriptor);
}