using Unity.Plastic.Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Diagnostics;

public class AutomationHealthCheckClient
{
    private static readonly HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(15) };
    private const string Endpoint = "https://plwyuwqm28.execute-api.sa-east-1.amazonaws.com/automationHealthCheck";

    static string repoName = Environment.GetEnvironmentVariable("PLASTIC_REPO");
    static string buildTarget = Environment.GetEnvironmentVariable("BUILD_TARGET");

    public static void SendHealthCheck(object payload)
    {
        var corr = Guid.NewGuid().ToString("N").Substring(0, 8);
        var sw = Stopwatch.StartNew();

        try
        {
            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"[HC][{corr}] POST {Endpoint}");
            Console.WriteLine($"[HC][{corr}] Payload bytes={Encoding.UTF8.GetByteCount(json)}");
            Console.WriteLine($"[HC][{corr}] Timeout={httpClient.Timeout.TotalSeconds}s");


            HttpResponseMessage response = httpClient.PostAsync(Endpoint, content).GetAwaiter().GetResult();
            string result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            sw.Stop();

            var amznReqId = response.Headers.Contains("x-amzn-RequestId") ? string.Join(",", response.Headers.GetValues("x-amzn-RequestId")) : "";
            var apigwId = response.Headers.Contains("x-amz-apigw-id") ? string.Join(",", response.Headers.GetValues("x-amz-apigw-id")) : "";


            Console.WriteLine($"[HC][{corr}] Status: {(int)response.StatusCode} {response.ReasonPhrase} (HTTP/{response.Version}) in {sw.ElapsedMilliseconds}ms");
            if (!string.IsNullOrEmpty(amznReqId)) Console.WriteLine($"[HC][{corr}] x-amzn-RequestId: {amznReqId}");
            if (!string.IsNullOrEmpty(apigwId)) Console.WriteLine($"[HC][{corr}] x-amz-apigw-id: {apigwId}");
            Console.WriteLine($"[HC][{corr}] Response bytes={Encoding.UTF8.GetByteCount(result)}");
            Console.WriteLine($"[HC][{corr}] Response: {Truncate(result, 2000)}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine($"[HC][{corr}] Erro ao enviar HealthCheck após {sw.ElapsedMilliseconds}ms: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[HC][{corr}] Stack: {ex.StackTrace}");
        }
    }

    public static void SendHealthy()
    {
        SendHealthCheck(new { 
            status = "healthy",
            projectId = repoName,
            platform = buildTarget,
            step = "ucb_postExport"
        });
    }

    public static void SendWarning(string message)
    {
        SendHealthCheck(new {
            status = "warning",
            projectId = repoName,
            platform = buildTarget,
            message,
            step = "ucb_postExport"
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
            stack,
            step = "ucb_postExport"
        });
    }
    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Length <= max ? s : s.Substring(0, max) + $"... [truncated {s.Length - max} chars]";
    }
}
