# TestDaprGrpcSubscripber

## Dapr实现.Net Grpc服务之间的发布和订阅，并采用WebApi类似的事件订阅方式的示例代码。

通过微服务ServiceA发布事件，然后在ServiceB接收事件并处理。

为了保持WebApi和Grpc事件订阅代码的一致性，Dapr Side Care 与微服务Grpc通讯的情况下实现如下写法来订阅并处理事件。

```csharp
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
```

其原理是配置builder.Services.AddGrpc().AddJsonTranscoding(); 然后在Grpc方法Proto文件中配置google.api.http 选项，增加Grpc方法将转码为JSON WebApi调用。

```go
option (google.api.http) = {
      post: "/v1/greeter/testtopicevent",
      body: "eventData"
    };
```

然后重写Dapr提供的AppCallback.AppCallbackBaseGrpc的OnTopicEvent方法，通过httpClient动态调用该Web Api。

详细讲解请访问我的博客：https://www.cnblogs.com/xiaozhuang/p/16832868.html

