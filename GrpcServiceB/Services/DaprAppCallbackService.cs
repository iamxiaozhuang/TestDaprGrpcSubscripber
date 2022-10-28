using Dapr.AppCallback.Autogen.Grpc.v1;
using Dapr.Client.Autogen.Grpc.v1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Dapr.AppCallback.Autogen.Grpc.v1.TopicEventResponse.Types;

namespace GrpcServiceB.Services
{
    public class DaprAppCallbackService : AppCallback.AppCallbackBase
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly EndpointDataSource _endpointDataSource;
        private readonly HttpClient _httpClient4TopicEvent;

        public DaprAppCallbackService(ILoggerFactory loggerFactory, EndpointDataSource endpointDataSource, IHttpClientFactory httpClientFactory)
        {
            _loggerFactory = loggerFactory;
            _endpointDataSource = endpointDataSource;
            _httpClient4TopicEvent = httpClientFactory.CreateClient("HttpClient4TopicEvent");
        }

        

        public async override Task<ListTopicSubscriptionsResponse> ListTopicSubscriptions(Empty request, ServerCallContext context)
        {
            var result = new ListTopicSubscriptionsResponse();

            var subcriptions = _endpointDataSource.GetDaprSubscriptions(_loggerFactory);
            foreach (var subscription in subcriptions)
            {
                TopicSubscription subscr = new TopicSubscription()
                {
                    PubsubName = subscription.PubsubName,
                    Topic = subscription.Topic,
                    Routes = new TopicRoutes()
                };
                subscr.Routes.Default = subscription.Route;
                result.Subscriptions.Add(subscr);
            }
            return result;
        }

        public async override Task<TopicEventResponse> OnTopicEvent(TopicEventRequest request, ServerCallContext context)
        {
            TopicEventResponse topicResponse = new TopicEventResponse();
            string payloadString = request.Data.ToStringUtf8();
            Console.WriteLine("OnTopicEvent Data：" + payloadString);
            
            HttpContent postContent = new StringContent(payloadString, new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await _httpClient4TopicEvent.PostAsync("http://" + context.Host + "/" + request.Path, postContent);
            string responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseContent);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("OnTopicEvent Invoke Success.");
                topicResponse.Status = TopicEventResponseStatus.Success;
            }
            else
            {
                Console.WriteLine("OnTopicEvent Invoke Error.");
                topicResponse.Status = TopicEventResponseStatus.Drop;
            }
            return topicResponse;
        }
    }
}
