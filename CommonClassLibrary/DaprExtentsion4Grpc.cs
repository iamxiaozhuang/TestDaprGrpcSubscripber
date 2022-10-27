using Dapr.Client;
using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonClassLibrary
{
    public static class DaprExtentsion4Grpc
    {
        public static IGrpcServerBuilder AddDapr(this IGrpcServerBuilder builder, Action<DaprClientBuilder> configureClient = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // This pattern prevents registering services multiple times in the case AddDapr is called
            // by non-user-code.
            if (builder.Services.Any(s => s.ImplementationType == typeof(DaprMvcMarkerService)))
            {
                return builder;
            }

            builder.Services.AddDaprClient(configureClient);

            builder.Services.AddSingleton<DaprMvcMarkerService>();

            return builder;
        }


        private class DaprMvcMarkerService
        {
        }
    }
}
