using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Routing.Patterns;
using System.Data;
using System.Text.Json.Serialization;
using System.Text.Json;
using Google.Api;

namespace GrpcServiceB
{
    public static class MapSubscribeHandler4GrpcExtension
    {

        public static List<Subscription> AllSubcriptions {get;set;}


        public static List<Subscription> GetDaprSubscriptions(this EndpointDataSource dataSource, ILoggerFactory loggerFactory, SubscribeOptions options = null)
        {
            if (AllSubcriptions != null)
            {
                return AllSubcriptions;
            }
            else
            {
                var logger = loggerFactory.CreateLogger("DaprTopicSubscription");
                var subscriptions = dataSource.Endpoints
                    .OfType<RouteEndpoint>()
                    .Where(e => e.Metadata.GetOrderedMetadata<ITopicMetadata>().Any(t => t.Name != null)) // only endpoints which have TopicAttribute with not null Name.
                    .SelectMany(e =>
                    {
                        var topicMetadata = e.Metadata.GetOrderedMetadata<ITopicMetadata>();
                        var originalTopicMetadata = e.Metadata.GetOrderedMetadata<IOriginalTopicMetadata>();

                        var subs = new List<(string PubsubName, string Name, string DeadLetterTopic, bool? EnableRawPayload, string Match, int Priority, Dictionary<string, string[]> OriginalTopicMetadata, string MetadataSeparator, RoutePattern RoutePattern)>();

                        for (int i = 0; i < topicMetadata.Count(); i++)
                        {
                            subs.Add((topicMetadata[i].PubsubName,
                                topicMetadata[i].Name,
                                (topicMetadata[i] as IDeadLetterTopicMetadata)?.DeadLetterTopic,
                                (topicMetadata[i] as IRawTopicMetadata)?.EnableRawPayload,
                                topicMetadata[i].Match,
                                topicMetadata[i].Priority,
                                originalTopicMetadata.Where(m => (topicMetadata[i] as IOwnedOriginalTopicMetadata)?.OwnedMetadatas?.Any(o => o.Equals(m.Id)) == true || string.IsNullOrEmpty(m.Id))
                                                     .GroupBy(c => c.Name)
                                                     .ToDictionary(m => m.Key, m => m.Select(c => c.Value).Distinct().ToArray()),
                                (topicMetadata[i] as IOwnedOriginalTopicMetadata)?.MetadataSeparator,
                                e.RoutePattern));
                        }

                        return subs;
                    })
                    .Distinct()
                    .GroupBy(e => new { e.PubsubName, e.Name })
                    .Select(e => e.OrderBy(e => e.Priority))
                    .Select(e =>
                    {
                        var first = e.First();
                        var rawPayload = e.Any(e => e.EnableRawPayload.GetValueOrDefault());
                        var metadataSeparator = e.FirstOrDefault(e => !string.IsNullOrEmpty(e.MetadataSeparator)).MetadataSeparator ?? ",";
                        var rules = e.Where(e => !string.IsNullOrEmpty(e.Match)).ToList();
                        var defaultRoutes = e.Where(e => string.IsNullOrEmpty(e.Match)).Select(e => RoutePatternToString(e.RoutePattern)).ToList();
                        //var defaultRoute = defaultRoutes.FirstOrDefault();
                        var defaultRoute = defaultRoutes.LastOrDefault();

                        //multiple identical names. use comma separation.
                        var metadata = new Metadata(e.SelectMany(c => c.OriginalTopicMetadata).GroupBy(c => c.Key).ToDictionary(c => c.Key, c => string.Join(metadataSeparator, c.SelectMany(c => c.Value).Distinct())));
                        if (rawPayload || options?.EnableRawPayload is true)
                        {
                            metadata.Add(Metadata.RawPayload, "true");
                        }

                        if (logger != null)
                        {
                            if (defaultRoutes.Count > 1)
                            {
                                logger.LogError("A default subscription to topic {name} on pubsub {pubsub} already exists.", first.Name, first.PubsubName);
                            }

                            var duplicatePriorities = rules.GroupBy(e => e.Priority)
                              .Where(g => g.Count() > 1)
                              .ToDictionary(x => x.Key, y => y.Count());

                            foreach (var entry in duplicatePriorities)
                            {
                                logger.LogError("A subscription to topic {name} on pubsub {pubsub} has duplicate priorities for {priority}: found {count} occurrences.", first.Name, first.PubsubName, entry.Key, entry.Value);
                            }
                        }

                        var subscription = new Subscription()
                        {
                            Topic = first.Name,
                            PubsubName = first.PubsubName,
                            Metadata = metadata.Count > 0 ? metadata : null,
                        };

                        if (first.DeadLetterTopic != null)
                        {
                            subscription.DeadLetterTopic = first.DeadLetterTopic;
                        }

                        // Use the V2 routing rules structure
                        if (rules.Count > 0)
                        {
                            subscription.Routes = new Routes
                            {
                                Rules = rules.Select(e => new Rule
                                {
                                    Match = e.Match,
                                    Path = RoutePatternToString(e.RoutePattern),
                                }).ToList(),
                                Default = defaultRoute,
                            };
                        }
                        // Use the V1 structure for backward compatibility.
                        else
                        {
                            subscription.Route = defaultRoute;
                        }

                        return subscription;
                    })
                    .OrderBy(e => (e.PubsubName, e.Topic));

                AllSubcriptions = subscriptions.ToList();

                return AllSubcriptions;
            }
        }

        private static string RoutePatternToString(RoutePattern routePattern)
        {
            return string.Join("/", routePattern.PathSegments
                                    .Select(segment => string.Concat(segment.Parts.Cast<RoutePatternLiteralPart>()
                                    .Select(part => part.Content))));
        }

        

    }

    public class Subscription
    {
        /// <summary>
        /// Gets or sets the topic name.
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// Gets or sets the pubsub name
        /// </summary>
        public string PubsubName { get; set; }

        /// <summary>
        /// Gets or sets the route
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets or sets the routes
        /// </summary>
        public Routes Routes { get; set; }

        /// <summary>
        /// Gets or sets the metadata.
        /// </summary>
        public Metadata Metadata { get; set; }

        /// <summary>
        /// Gets or sets the deadletter topic.
        /// </summary>
        public string DeadLetterTopic { get; set; }
    }

    /// <summary>
    /// This class defines the metadata for subscribe endpoint.
    /// </summary>
    public class Metadata : Dictionary<string, string>
    {
        public Metadata() { }

        public Metadata(IDictionary<string, string> dictionary) : base(dictionary) { }

        /// <summary>
        /// RawPayload key
        /// </summary>
        internal const string RawPayload = "rawPayload";
    }

    public class Routes
    {
        /// <summary>
        /// Gets or sets the default route
        /// </summary>
        public string Default { get; set; }

        /// <summary>
        /// Gets or sets the routing rules
        /// </summary>
        public List<Rule> Rules { get; set; }
    }

    public class Rule
    {
        /// <summary>
        /// Gets or sets the CEL expression to match this route.
        /// </summary>
        public string Match { get; set; }

        /// <summary>
        /// Gets or sets the path of the route.
        /// </summary>
        public string Path { get; set; }
    }

}
