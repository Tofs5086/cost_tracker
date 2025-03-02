
# Azure Cost Tracker with C#

This project demonstrates how to create a basic cost tracker that fetches daily Azure spending data using C# and displays it in a console table. Additionally, it includes an example of implementing interactive authentication using MSAL.NET (Microsoft Authentication Library for .NET).

---

## Features

- **Fetch Azure Spending Data**: Retrieve daily Azure consumption data using the Azure Consumption API.
- **Display Data in Console**: Present the data in a formatted console table.
- **Interactive Authentication**: Authenticate users interactively using MSAL.NET for secure access to Azure resources.

---

## Prerequisites

Before you begin, ensure you have the following:

1. **Azure Subscription**: An active Azure subscription.
2. **Azure CLI**: Install and authenticate using the Azure CLI (`az login`).
3. **Azure Consumption API**: Familiarize yourself with the [Azure Consumption API](https://learn.microsoft.com/en-us/rest/api/consumption/).
4. **C# Development Environment**: Install the .NET SDK and an IDE like Visual Studio or Visual Studio Code.

---

## Setup and Usage

### 1. Azure AD App Registration
1. Go to the Azure portal.
2. Navigate to **Azure Active Directory > App registrations > New registration**.
3. Register a new application and note the **Client ID** and **Tenant ID**.
4. Under **Certificates & secrets**, create a new client secret and save it securely.

### 2. Grant API Permissions
1. In the app registration, go to **API permissions > Add a permission**.
2. Select **Azure Cost Management API** and grant the `Consumption.Read` permission.
3. Click **Grant admin consent**.

### 3. Install Required NuGet Packages
Install the following NuGet packages in your C# project:
- `Microsoft.Identity.Client` for authentication.
- `Newtonsoft.Json` for JSON parsing.
- `ConsoleTables` for displaying data in a table format.

Run these commands in the terminal:
```bash
dotnet add package Microsoft.Identity.Client
dotnet add package Newtonsoft.Json
dotnet add package ConsoleTables
```

### 4. Write the C# Code
Below is an example implementation for fetching and displaying Azure cost data:

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using ConsoleTables;

class Program
{
    private static string clientId = "YOUR_CLIENT_ID";
    private static string tenantId = "YOUR_TENANT_ID";
    private static string clientSecret = "YOUR_CLIENT_SECRET";
    private static string subscriptionId = "YOUR_SUBSCRIPTION_ID";

    private static string authority = $"https://login.microsoftonline.com/{tenantId}";
    private static string[] scopes = { "https://management.azure.com/.default" };

    static async Task Main(string[] args)
    {
        var app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri(authority))
            .Build();

        var authResult = await app.AcquireTokenForClient(scopes).ExecuteAsync();
        string accessToken = authResult.AccessToken;

        await FetchDailyCostData(accessToken);
    }

    static async Task FetchDailyCostData(string accessToken)
    {
        string endpoint = $"https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.Consumption/usageDetails?api-version=2021-10-01";

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject result = JObject.Parse(jsonResponse);
                var table = new ConsoleTable("Date", "Service", "Cost");

                foreach (var item in result["value"])
                {
                    string date = item["properties"]["usageStart"].ToString();
                    string service = item["properties"]["meterDetails"]["meterName"].ToString();
                    string cost = item["properties"]["pretaxCost"].ToString();

                    table.AddRow(date, service, cost);
                }

                table.Write();
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
        }
    }
}
```

### 5. Run the Application
1. Replace `YOUR_CLIENT_ID`, `YOUR_TENANT_ID`, `YOUR_CLIENT_SECRET`, and `YOUR_SUBSCRIPTION_ID` with your actual Azure credentials.
2. Run the application using `dotnet run`.

### 6. Output
The console will display a table with the following columns:
- **Date**: The date of the usage.
- **Service**: The Azure service name.
- **Cost**: The cost incurred for that service on the given date.

---

## Interactive Authentication with MSAL.NET

To implement interactive authentication, follow these steps:

### 1. Set Up the Authentication Configuration
Create a configuration file or class to store your Azure AD app details.

```csharp
public class AzureAdConfig
{
    public string ClientId { get; set; }
    public string TenantId { get; set; }
    public string RedirectUri { get; set; }
    public string Authority => $"https://login.microsoftonline.com/{TenantId}";
}
```

### 2. Initialize the MSAL Public Client Application
Use the `PublicClientApplicationBuilder` to create an instance of the MSAL public client application.

```csharp
using Microsoft.Identity.Client;

public class AuthService
{
    private readonly IPublicClientApplication _publicClientApp;

    public AuthService(AzureAdConfig config)
    {
        _publicClientApp = PublicClientApplicationBuilder.Create(config.ClientId)
            .WithAuthority(config.Authority)
            .WithRedirectUri(config.RedirectUri)
            .Build();
    }
}
```

### 3. Acquire a Token Interactively
Use the `AcquireTokenInteractive` method to prompt the user to sign in and acquire an access token.

```csharp
public async Task<string> AcquireTokenAsync(IEnumerable<string> scopes)
{
    try
    {
        var result = await _publicClientApp.AcquireTokenInteractive(scopes)
            .ExecuteAsync();

        Console.WriteLine("Token acquired successfully!");
        return result.AccessToken;
    }
    catch (MsalException ex)
    {
        Console.WriteLine($"Error acquiring token: {ex.Message}");
        throw;
    }
}
```

### 4. Use the Token
Once you have the token, you can use it to call protected APIs.

```csharp
public async Task CallApiAsync(string accessToken)
{
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

    var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");
    var content = await response.Content.ReadAsStringAsync();

    Console.WriteLine("API Response:");
    Console.WriteLine(content);
}
```

### 5. Put It All Together
Here’s how you can use the above classes in your application:

```csharp
class Program
{
    private static async Task Main(string[] args)
    {
        var config = new AzureAdConfig
        {
            ClientId = "YOUR_CLIENT_ID",
            TenantId = "YOUR_TENANT_ID",
            RedirectUri = "http://localhost" // Or your app’s redirect URI
        };

        var authService = new AuthService(config);

        // Define the scopes you need
        var scopes = new[] { "User.Read" }; // Example: Microsoft Graph’s User.Read scope

        try
        {
            // Acquire the token interactively
            var token = await authService.AcquireTokenAsync(scopes);

            // Use the token to call an API
            await authService.CallApiAsync(token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
```

---

## Notes

- The Azure Consumption API may have rate limits. Handle exceptions and retries accordingly.
- For production use, securely store and manage secrets (e.g., using Azure Key Vault).
- You can extend this application to filter data by date range, group by service, or export to a file.

---

## Example Output

When you run the application:
1. A browser window will open, prompting the user to sign in.
2. After successful authentication, the access token will be used to call the Microsoft Graph API.
3. The API response (e.g., user profile data) will be displayed in the console.

