using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.Net.Http;

namespace OrderItemsReserver
{
    public static class ReservationOfOrderItems
    {
        [FunctionName("ReservationOfOrderItems")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a reservation of order items.");

            var blobStorageUrl = Environment.GetEnvironmentVariable("WEBSITE_CONTENTAZUREFILECONNECTIONSTRING");
            var blobServiceClient = new BlobServiceClient(blobStorageUrl);
            
            string containerName = "orderitemscontainer2";
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            containerClient.CreateIfNotExists();

            string name = "orderitems" + Guid.NewGuid().ToString() + ".json";
            var blobClient = containerClient.GetBlobClient(name);

            // Upload the blob
            var content = await req.Content.ReadAsStreamAsync();
            await blobClient.UploadAsync(content).ConfigureAwait(false);

            return new OkObjectResult("This HTTP triggered function executed successfully.");
        }
    }
}
