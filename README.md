# TestDaprGrpcSubscripber

这是一个测试 Dapr SideCar 通过 Grpc 与 .Net Core 7.0 App 通讯, 实现分事件发布和订阅的示例代码。
事件通过ServiceA发布事件，然后ServiceB接收事件并处理。
该示例实现了通用的Grpc接受事件处理，其原理是配置builder.Services.AddGrpc().AddJsonTranscoding()
在Grpc方法配置google.api.http，实现将Grpc方法将转码为WebApi调用。
option (google.api.http) = {
      post: "/v1/greeter/testtopicevent",
      body: "eventData"
    };
然后在Dapr提供的AppCallback.AppCallbackBaseGrpc的OnTopicEvent方法实现中，通过httpClient调用该Api。
