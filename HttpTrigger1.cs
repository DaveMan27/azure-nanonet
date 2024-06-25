
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function
{
    public class HttpTrigger1
    {
        private readonly ILogger<HttpTrigger1> _logger;

        public HttpTrigger1(ILogger<HttpTrigger1> logger)
        {
            _logger = logger;
        }

        private static readonly string apiUrl = "https://eu-open.nanonets.com/api/v2/OCR/Model/73e82f7d-38dd-4c4e-b6b7-b89b4743130a/LabelUrls/?async=false";
        private static readonly string apiKey = "191abf3c-12c5-11ef-bf2f-7e572644319b:";

        private static readonly Dictionary<string, string> modelTypeMap = new Dictionary<string, string>
        {
            { "receipts", "73e82f7d-38dd-4c4e-b6b7-b89b4743130a" },
            { "BL", "bae0ec8b-10ad-4809-9345-e4b2c2922852" },
            { "visa", "7e3880cd-aa0d-488f-8e72-767ac1abad54" }
        };

        [Function("HttpTrigger1")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            //_logger.LogInformation("C# HTTP trigger function processed a request.");

            string imageUrl = "https://postman.com/_aether-assets/illustrations/dark/illustration-hit-send.svg";
            Console.WriteLine(modelTypeMap["receipts"]);
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes(apiKey)));

            // Prepare the request
            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            // Example payload
            var formData = new FormUrlEncodedContent(new[]
                {
                        new KeyValuePair<string, string>("urls", imageUrl)
                    }
            );

            // Add content to the request
            request.Content = formData;

            // Send the request
            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                // Read and display the response
                string result = await response.Content.ReadAsStringAsync();
                Console.WriteLine(result);
            }
            else
            {
                Console.WriteLine($"Error: {response.ReasonPhrase}");
            }
        }
        //return new OkObjectResult("Welcome to Azure Functions!");
    }
}

