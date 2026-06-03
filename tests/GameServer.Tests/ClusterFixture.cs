using GameServer.Abstractions;
using Microsoft.Extensions.Configuration;
using Orleans.TestingHost;

namespace GameServer.Tests;

/// <summary>테스트 사일로 구성: 인메모리 grain 스토리지 + 인메모리 스트림.</summary>
public sealed class TestSiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder
            .AddMemoryGrainStorage("playerStore")
            .AddMemoryGrainStorage("PubSubStore")
            .AddMemoryStreams(GameStreams.ProviderName);
    }
}

/// <summary>테스트 클라이언트도 동일한 인메모리 스트림 프로바이더를 구독할 수 있게 구성.</summary>
public sealed class TestClientConfigurator : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        => clientBuilder.AddMemoryStreams(GameStreams.ProviderName);
}

/// <summary>xUnit 컬렉션 전체에서 공유하는 TestCluster.</summary>
public sealed class ClusterFixture : IDisposable
{
    public TestCluster Cluster { get; }

    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
        builder.AddClientBuilderConfigurator<TestClientConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose() => Cluster.StopAllSilos();
}

[CollectionDefinition(ClusterCollection.Name)]
public sealed class ClusterCollection : ICollectionFixture<ClusterFixture>
{
    public const string Name = "ClusterCollection";
}
