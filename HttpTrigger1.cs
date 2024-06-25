/*using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Company.Function
{
    public class HttpTrigger1
    {

        //private static readonly string apiUrl = "https://eu-open.nanonets.com/api/v2/OCR/Model/73e82f7d-38dd-4c4e-b6b7-b89b4743130a/LabelUrls/?async=false";
        private static readonly string apiKey = "191abf3c-12c5-11ef-bf2f-7e572644319b:";

        private static readonly Dictionary<string, string> modelTypeMap = new Dictionary<string, string>
        {
            { "receipts", "73e82f7d-38dd-4c4e-b6b7-b89b4743130a" },
            { "BL", "bae0ec8b-10ad-4809-9345-e4b2c2922852" },
            { "visa", "7e3880cd-aa0d-488f-8e72-767ac1abad54" }
        };

        [Function("HttpTrigger1")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            //log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string modelType = data.modelType;
            string imageUrl = data.imageUrl;
            string apiUrl = $"https://eu-open.nanonets.com/api/v2/OCR/Model/{modelTypeMap[modelType]}/LabelUrls/?async=false";
            //log.LogInformation("URL: " + apiUrl);



            //string imageUrl = "https://postman.com/_aether-assets/illustrations/dark/illustration-hit-send.svg";
            //Console.WriteLine(modelTypeMap["receipts"]);
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes(apiKey)));
            // Prepare the request
            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            //Example payload
            var formData = new Dictionary<string, string>{{"urls", imageUrl }};
            request.Content = new FormUrlEncodedContent(formData);




            // Add content to the request
            //request.Content = formData;

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
            return new OkObjectResult(response);
        }
    }
}*/


using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;

namespace Company.Function
{
    public class HttpTrigger1
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string apiKey = "191abf3c-12c5-11ef-bf2f-7e572644319b:";
        private static readonly Dictionary<string, string> modelTypeMap = new Dictionary<string, string>
        {
            { "receipts", "73e82f7d-38dd-4c4e-b6b7-b89b4743130a" },
            { "BL", "bae0ec8b-10ad-4809-9345-e4b2c2922852" },
            { "visa", "7e3880cd-aa0d-488f-8e72-767ac1abad54" }
        };

        private const string AllowedOrigin = "*";


        //private ILogger log;

        [Function("HttpTrigger1")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            SetCorsHeaders(req.HttpContext.Response);
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string modelType = data?.modelType;
            string imageUrl = data?.imageUrl;

            //log.LogInformation("" + modelType + " : " + imageUrl);

            if (string.IsNullOrWhiteSpace(modelType) || !modelTypeMap.ContainsKey(modelType))
            {
                //log.LogError("Invalid or missing model type.");
                return new BadRequestObjectResult("Invalid or missing model type.");
            }

            string apiUrl = $"https://eu-open.nanonets.com/api/v2/OCR/Model/{modelTypeMap[modelType]}/LabelUrls/?async=false";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes(apiKey)));


            var formData = new Dictionary<string, string> { { "urls", imageUrl } };
            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = new FormUrlEncodedContent(formData)
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                //log.LogInformation("OCR request successful.");
                return new OkObjectResult(result);
            }
            else
            {
                //log.LogError($"OCR request failed with status code {response.StatusCode}");
                //return new ObjectResult(response.ReasonPhrase) { StatusCode = (int)response.StatusCode };
                return new ObjectResult(response);
            }
        }

        private static void SetCorsHeaders(HttpResponse response)
        {
            // Add CORS headers to the response
            response.Headers.Add("Access-Control-Allow-Origin", AllowedOrigin);
            response.Headers.Add("Access-Control-Allow-Methods", "POST"); // Add the allowed methods.
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization"); // Add the allowed headers.            
        }
    }
}


