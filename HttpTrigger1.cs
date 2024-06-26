using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Company.Function
{

    public class HttpTrigger1
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly Dictionary<string, string> modelTypeMap = new Dictionary<string, string>
        {
            { "receipts", "73e82f7d-38dd-4c4e-b6b7-b89b4743130a" },
            { "BL", "bae0ec8b-10ad-4809-9345-e4b2c2922852" },
            { "visa", "7e3880cd-aa0d-488f-8e72-767ac1abad54" }
        };
        private class ReturnedResponse
        {
            public string ResponseMessage { get; set; }
            public int ResponseCode { get; set; }
        }

        private const string AllowedOrigin = "*";

        [Function("HttpTrigger1")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        FunctionContext executionContext)
        {

            var log = executionContext.GetLogger("HttpTrigger1");

            ReturnedResponse rr = new ReturnedResponse();
            try
            {
                //Retreive data from trigger request
                SetCorsHeaders(req.HttpContext.Response);
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                string? modelType = data?.modelType;
                string? imageBase64 = data?.imageBase64;

                //Retrieve secret from vault
                const string secretName = "Nanonet";
                var keyVaultName = "jafi-eu-crm-kv";
                var kvUri = $"https://{keyVaultName}.vault.azure.net";
                var clientSecretRetrieval = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
                var secret = await clientSecretRetrieval.GetSecretAsync(secretName);
                string apiKey = secret.Value.Value.ToString();

                if (string.IsNullOrEmpty(modelType) || string.IsNullOrEmpty(imageBase64))
                {
                    rr.ResponseMessage = "Ensure that valid data has been passed to the function";
                    rr.ResponseCode = 400;
                    return new BadRequestObjectResult(rr);
                }

                log.LogInformation($"{modelType} : {imageBase64}");

                if (!modelTypeMap.ContainsKey(modelType))
                {
                    rr.ResponseMessage = "Invalid or missing model type.";
                    rr.ResponseCode = 400;
                    return new BadRequestObjectResult(rr);
                }

                if (string.IsNullOrEmpty(apiKey))
                {
                    rr.ResponseMessage = "Invalid API authorization";
                    rr.ResponseCode = 401;
                    return new BadRequestObjectResult(rr);
                }

                //Callout to Nanonet
                string apiUrl = $"https://eu-open.nanonets.com/api/v2/OCR/Model/{modelTypeMap[modelType]}/LabelUrls/?async=false";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes(apiKey)));
                var formData = new Dictionary<string, string> { { "base64_data", imageBase64 } };
                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Content = new FormUrlEncodedContent(formData)
                };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    log.LogInformation("OCR request successful.");
                    return new OkObjectResult(result);
                }
                else
                {
                    log.LogError($"OCR request failed with status code {response.StatusCode}");
                    rr.ResponseMessage = response.ReasonPhrase;
                    rr.ResponseCode = (int)response.StatusCode;
                    return new BadRequestObjectResult(rr);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while processing the request.");
                rr.ResponseMessage = ex.Message;
                rr.ResponseCode = 500;
                return new ObjectResult(rr) { StatusCode = 500 };
            }
        }

        private static void SetCorsHeaders(HttpResponse response)
        {
            response.Headers.Add("Access-Control-Allow-Origin", AllowedOrigin);
            response.Headers.Add("Access-Control-Allow-Methods", "POST");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        }
    }
}