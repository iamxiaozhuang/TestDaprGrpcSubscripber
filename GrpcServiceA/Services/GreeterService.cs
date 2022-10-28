using CommonClassLibrary;
using Dapr.Client;
using Grpc.Core;
using GrpcServiceA;

namespace GrpcServiceA.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        private readonly DaprClient _daprClient;

        public GreeterService(ILogger<GreeterService> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }

        public async override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            //await _daprClient.SaveStateAsync("statestore", "testKey", request.Name);
            EventData eventData = new EventData() { Id = 6, Name = request.Name, Description = "Looking for a job" };
            await _daprClient.PublishEventAsync<EventData>("pubsub", "TestTopic", eventData);
            return new HelloReply
            {
                Message = "Hello" + request.Name
            };
        }
    }
}