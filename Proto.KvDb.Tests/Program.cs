// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Proto.KvDb.GrpcService;

async Task TestUsingGrpc()
{
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
    await Parallel.ForEachAsync(Enumerable.Range(0, 20000), async (i, token) =>
    {
        var sw = new Stopwatch();
        sw.Start();
        await client.SetAsync(new SetRequest()
        {
            Key = string.Format("member-{0}", i.ToString()),
            Value = i.ToString()
        });
        sw.Stop();

        Console.WriteLine($"{i} ===> {sw.Elapsed.TotalSeconds} seconds");
    });
}

async Task TestUsingHttp()
{
    using (var httpClient = new HttpClient())
    {
        await Parallel.ForEachAsync(Enumerable.Range(0, 20000), async (i, token) =>
        {
            using var request = new HttpRequestMessage(new HttpMethod("POST"), "http://192.168.15.200:5059/api/Db/set");
            request.Headers.TryAddWithoutValidation("accept", "text/plain");

            var content = $"{{\n  \"key\": \"{i}\",\n  \"value\": \"{i}\"\n}}";
            request.Content = new StringContent(content);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            var sw = new Stopwatch();
            sw.Start();
            var response = await httpClient.SendAsync(request, token);
            sw.Stop();

            Console.WriteLine($"{i} ===> {sw.Elapsed.TotalSeconds} seconds");
        });
    }
}

await TestUsingHttp();