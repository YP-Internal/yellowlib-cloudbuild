using Unity.Plastic.Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;

public class AutomationHealthCheckClient
{
    private static readonly HttpClient httpClient = new HttpClient();
    private const string Endpoint = "https://plwyuwqm28.execute-api.sa-east-1.amazonaws.com/automationHealthCheck";

    static string repoName = Environment.GetEnvironmentVariable("PLASTIC_REPO");
    static string buildTarget = Environment.GetEnvironmentVariable("BUILD_TARGET");

    public static void SendHealthCheck(object payload)
    {
        try
        {
            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = httpClient.PostAsync(Endpoint, content).GetAwaiter().GetResult();
            string result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            Console.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");
            Console.WriteLine($"Response: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar HealthCheck: {ex.Message}");
        }
    }

    public static void SendHealthy()
    {
        SendHealthCheck(new { 
            status = "healthy",
            projectId = repoName,
            platform = buildTarget
        });
    }

    public static void SendWarning(string message)
    {
        SendHealthCheck(new {
            status = "warning",
            projectId = repoName,
            platform = buildTarget,
            message
        });
    }

    public static void SendError(string message, string stack)
    {
        SendHealthCheck(new
        {
            status = "error",
            projectId = repoName,
            platform = buildTarget,
            message,
            stack
        });
    }
}
