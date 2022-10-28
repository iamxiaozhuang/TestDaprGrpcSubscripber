using Dapr;
using Grpc.Core;
using GrpcServiceB;

namespace GrpcServiceB.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }

        [Topic("pubsub", "TestTopic")]
        public override Task<HelloReply> TestTopicEvent(TestTopicEventRequest request, ServerCallContext context)
        {
            string message = "TestTopicEvent" + request.EventData.Name;
            Console.WriteLine(message);
            return Task.FromResult(new HelloReply
            {
                Message = message
            });
        }
    }
}