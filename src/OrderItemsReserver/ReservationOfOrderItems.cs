using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.Net.Http;
using System.IO;
using Microsoft.Azure.ServiceBus;
using System.Threading;
using System.Text.Json;
using System.Text;

namespace OrderItemsReserver
{
    public static class ReservationOfOrderItems
    {
        static IQueueClient _queueClient;

        [FunctionName("ReservationOfOrderItems")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a reservation of order items.");

            await ServiceBusOrderReceiver();

            return new OkObjectResult("This HTTP triggered function executed successfully.");
        }

        private static async Task ServiceBusOrderReceiver()
        {
            var serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");
            var queueName = Environment.GetEnvironmentVariable("ServiceBusQueueName");

            if (_queueClient != null)
            {
                await _queueClient.CloseAsync();
            }
            _queueClient = new QueueClient(serviceBusConnectionString, queueName);

            _queueClient.RegisterMessageHandler(ProcessMessagesAsync
             , new MessageHandlerOptions(ErrorHandlerSendEmail) { MaxConcurrentCalls = 1, AutoComplete = false }
            );
        }

        private static async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            await SaveOrderIntoBlob(new MemoryStream(message.Body));
            await _queueClient.CompleteAsync(message.SystemProperties.LockToken);
        }

        static Task ErrorHandler(ExceptionReceivedEventArgs args)
        {
            var context = args.ExceptionReceivedContext;
            Console.WriteLine($"Message handler encountered an exception {args.Exception}.");
            Console.WriteLine("Exception context for troubleshooting:");
            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }

        static async Task ErrorHandlerSendEmail(ExceptionReceivedEventArgs args)
        {
            var jsonData = JsonSerializer.Serialize(new
            {
                email = "dzmitry_rudleuski@epam.com",
                due = $"28/3/2022. Message handler encountered an exception {args.Exception}.",
                task = "Service Bus Azure Homework"
            });

            var emailNotificationApplication = Environment.GetEnvironmentVariable("EmailNotificationApplication");
            if (string.IsNullOrEmpty(emailNotificationApplication))
            {
                return;
            }

            var client = new HttpClient();
            var result = await client.PostAsync(emailNotificationApplication, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                await ErrorHandler(args);
            }
        }

        private static async Task SaveOrderIntoBlob(Stream orderRequest)
        {
            // get blob service client
            var blobStorageUrl = Environment.GetEnvironmentVariable("WEBSITE_CONTENTAZUREFILECONNECTIONSTRING");
            var options = new BlobClientOptions();
            options.Retry.MaxRetries = 3;
            var blobServiceClient = new BlobServiceClient(blobStorageUrl, options);

            // get blob Container client
            string containerName = "orderitemscontainer2";
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            containerClient.CreateIfNotExists();

            // get blob client 
            string name = "orderRequest" + Guid.NewGuid().ToString() + ".json";
            var blobClient = containerClient.GetBlobClient(name);

            // Upload the blob
            await blobClient.UploadAsync(orderRequest).ConfigureAwait(false);
        }
    }
}
