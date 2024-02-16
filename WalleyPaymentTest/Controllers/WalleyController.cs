using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WalleyPaymentTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalleyController : ControllerBase
    {

        [HttpGet]
        public async Task<IActionResult> WalleePaymentAsync()
        {
            // Construct the request payload
            var requestPayload = new
            {
                autoConfirmationEnabled = true,
                currency = "CHF",
                lineItems = new[]
                {
                new
                {
                    amountIncludingTax = "55",
                    name = "Test",
                    quantity = "1",
                    type = "PRODUCT",
                    uniqueId = "1"
                }
            }
            };

            // Serialize the request payload to JSON
            string jsonPayload = JsonSerializer.Serialize(requestPayload);

            // Create an instance of HttpClient
            using var httpClient = new HttpClient();

            // Define your request URI
            string requestUri = "https://app-wallee.com/api/transaction/create?spaceId=53455";

            // Create a HttpRequestMessage
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);

            // Set the request content type
            httpRequest.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Generate HMAC authorization headers
            string xMacVersion = "1.1";
            string xMacUserId = "96732";
            long timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            byte[] secretKey = Convert.FromBase64String("j6vInzIf2ScxkGNjP5XVYdBN1WcUj7OwXFwnIUZXT1s=");
            string xMacValue = GenerateHMAC(secretKey, timeStamp, "POST", requestUri);

            // Add headers
            httpRequest.Headers.Add("x-mac-version", xMacVersion);
            httpRequest.Headers.Add("x-mac-userid", xMacUserId);
            httpRequest.Headers.Add("x-mac-timestamp", timeStamp.ToString());
            httpRequest.Headers.Add("x-mac-value", xMacValue);

            // Send the request and get the response
            HttpResponseMessage response = await httpClient.SendAsync(httpRequest);

            string responseBodys = await response.Content.ReadAsStringAsync();


            // Check if the response is successful
            if (response.IsSuccessStatusCode)
            {
                return Ok();
            }
            else
            {
                JObject json = JObject.Parse(responseBodys);
                string defaultMessage = json["defaultMessage"].ToString();
                string message = json["message"].ToString();

                ObjectResult errorResult = new ObjectResult(new
                {
                    StatusCode = (int)response.StatusCode,
                    DefaultMessage = defaultMessage,
                    Message = message
                })
                {
                    StatusCode = (int)response.StatusCode
                };

                return errorResult;
            }
        }


        //private string GenerateHMAC(byte[] secretKey, decimal timestamp, string method, string uri)
        //{
        //    string concatenatedString = $"{"1"}|{"96732"}|{timestamp}|{method}|{uri}";
        //    string hashedData = string.Empty;

        //    // Ensure the correct separator is used
        //    concatenatedString = concatenatedString.Replace(',', '|');

        //    using (HMACSHA512 hmac = new HMACSHA512(secretKey))
        //    {
        //        byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenatedString));
        //        hashedData = Convert.ToBase64String(bytes);
        //    }

        //    return hashedData;

        //}

        //private string GenerateHMAC(byte[] secretKey, decimal timestamp, string method, string uri)
        //{
        //    method = "GET";
        //    string concatenatedString = $"1|96732|{timestamp}|{method}|{uri}";

        //    using (HMACSHA512 hmac = new HMACSHA512(secretKey))
        //    {
        //        byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenatedString));
        //        return Convert.ToBase64String(bytes);
        //    }
        //}


        private string GenerateHMAC(byte[] secretKey, decimal timestamp, string method, string uri)
        {
            string securedData = $"1|96732|{timestamp}|POST|{uri}";

            using (HMACSHA512 hmac = new HMACSHA512(secretKey))
            {
                byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(securedData));

                string result = Convert.ToBase64String(bytes);
                return result;
            }
        }
    }
}
