// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Net;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Proto.KvDb.GrpcService;

using var channel = GrpcChannel.ForAddress("http://192.168.15.200:51000", new GrpcChannelOptions()
{
    UnsafeUseInsecureChannelCallCredentials = true,
    HttpHandler = new SocketsHttpHandler()
    {
        EnableMultipleHttp2Connections = true,
        AutomaticDecompression = DecompressionMethods.All
    }
});

var client = new KeyValueDbService.KeyValueDbServiceClient(channel);
await Parallel.ForEachAsync(Enumerable.Range(0, 5000000), async (i, token) =>
{
    var sw = new Stopwatch();
    sw.Start();
    await client.SetAsync(new SetRequest()
    {
        Key = i.ToString(),
        Value = i.ToString()
    });
    sw.Stop();
    
    Console.WriteLine($"{i} ===> {sw.Elapsed.TotalSeconds} seconds");
});