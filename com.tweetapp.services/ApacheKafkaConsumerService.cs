using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using TweetMe.Models;

namespace TweetMe.com.tweetapp.services
{
    public class ApacheKafkaConsumerService : BackgroundService
    {
        private readonly string userTopic = "user";
        private readonly string tweetTopic = "tweet";
        private readonly string groupId = "test-consumer-group";
        private readonly string bootstrapServers = "localhost:9092";

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var config = new ConsumerConfig
            {
                GroupId = groupId,
                BootstrapServers = bootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            try
            {
                using (var consumerBuilder = new ConsumerBuilder
                <Ignore, string>(config).Build())
                {
                    //consumerBuilder.Subscribe(userTopic);
                    consumerBuilder.Subscribe(tweetTopic);
                    var cancelToken = new CancellationTokenSource();
                    int i = 0;

                    try
                    {
                        while (true)
                        {
                            var consumer = consumerBuilder.Consume(cancelToken.Token);
                            //var orderRequest = JsonSerializer.Deserialize<Tweet>(consumer.Message.Value);
                            //Debug.WriteLine($"Processing Order Id: { orderRequest.Id}");
                            Debug.WriteLine($"Processing Order Id: {i++}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        consumerBuilder.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            await Task.CompletedTask;
        }
        //public Task StopAsync(CancellationToken cancellationToken)
        //{
        //    return Task.CompletedTask;
        //}
    }
}
