// See https://aka.ms/new-console-template for more information
using Grpc.Core;
using Grpc.Net.Client;
using TestConsoleApp;

Console.WriteLine("Hello, World!");

var channel = GrpcChannel.ForAddress("http://localhost:50002"); // 服务端地址
var client = new Greeter.GreeterClient(channel);
var metadata = new Metadata
{
    { "dapr-app-id", "serviceA" }
};
var reply = await client.SayHelloAsync(new HelloRequest { Name = "test" }, metadata);
Console.WriteLine("Greeter 服务返回数据: " + reply.Message);


//Console.ReadKey();
