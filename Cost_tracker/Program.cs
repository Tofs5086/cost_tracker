using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;

namespace AzureCostTracker
{
    class Program
    {
        // Azure AD application credentials
        private static string clientId = "009f70e0-fe08-48c2-bec0-37903f9c7869"; 
        private static string tenantId = "19b5006c-0e02-4f3e-9e73-6f37cd0d4044"; 
        private static string subscriptionId = "23a13436-e9e3-4113-9428-bf08dabc3601"; 

        /// <summary>
        /// Main method - entry point of the application.
        /// It retrieves an access token and fetches daily Azure cost data.
        /// </summary>
        static async Task Main(string[] args)
        {
            // Get authentication token
            string token = await GetAccessTokenAsync();
            
            // Display the access token
            Console.WriteLine("\nAccess Token:\n");
            Console.WriteLine(token);
            Console.WriteLine("\n---------------------\n");

            // Fetch daily cost details from Azure
            await GetDailyCostAsync(token);
        }

        /// <summary>
        /// Retrieves an OAuth 2.0 access token for authenticating requests to Azure API.
        /// Uses interactive login to prompt the user for authentication.
        /// </summary>
        /// <returns>Access token as a string</returns>
        private static async Task<string> GetAccessTokenAsync()
        {
            // Create a public client application for authentication
            IPublicClientApplication app = PublicClientApplicationBuilder.Create(clientId)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                .WithRedirectUri("http://localhost") // Redirect URI for interactive login
                .Build();

            // Define the scope required for API access
            string[] scopes = { "https://management.azure.com/user_impersonation" }; // Delegated permission

            // Prompt user for authentication and obtain token
            AuthenticationResult result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
            
            // Return the access token
            return result.AccessToken;
        }

        /// <summary>
        /// Calls the Azure Consumption API to fetch daily cost details.
        /// </summary>
        /// <param name="token">Access token for authentication</param>
        private static async Task GetDailyCostAsync(string token)
        {
            // Create an HTTP client
            using (HttpClient client = new HttpClient())
            {
                // Set Authorization header with Bearer token
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Azure API endpoint for fetching usage details
                string url = $"https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.Consumption/usageDetails?api-version=2021-10-01";

                // Send GET request to Azure API
                HttpResponseMessage response = await client.GetAsync(url);

                // Check if response is successful
                if (response.IsSuccessStatusCode)
                {
                    // Read and parse JSON response
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JObject parsedResponse = JObject.Parse(jsonResponse);
                    
                    // Display cost details
                    DisplayCosts(parsedResponse);
                }
                else
                {
                    // Print error details if request fails
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {response.StatusCode}");
                    Console.WriteLine($"Details: {errorResponse}");
                }
            }
        }

        /// <summary>
        /// Parses and displays the daily cost data retrieved from the Azure API.
        /// </summary>
        /// <param name="parsedResponse">JSON object containing the cost details</param>
        private static void DisplayCosts(JObject parsedResponse)
        {
            Console.WriteLine("Date\t\tCost");
            Console.WriteLine("---------------------");

            // Check if the response contains cost data
            if (parsedResponse["value"] == null || !parsedResponse["value"].HasValues)
            {
                Console.WriteLine("No cost data found.");
                return;
            }

            // Iterate through cost data entries
            foreach (var item in parsedResponse["value"]!)
            {
                // Extract date and cost from response
                string? date = item["properties"]?["usageStart"]?.ToString()?.Split('T')[0];
                string? cost = item["properties"]?["pretaxCost"]?.ToString();

                // Print valid cost data or show error if missing
                if (date != null && cost != null)
                {
                    Console.WriteLine($"{date}\t{cost}");
                }
                else
                {
                    Console.WriteLine("Invalid or missing data in response.");
                }
            }
        }
    }
}
