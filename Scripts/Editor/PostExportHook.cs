#if UNITY_EDITOR
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using UnityEngine;

namespace YellowPanda.CloudBuild
{
    public class PostExportHook
    {
        static string repoName = Environment.GetEnvironmentVariable("PLASTIC_REPO");
        static string branchName = Environment.GetEnvironmentVariable("SCM_BRANCH");
        static string buildNumber = Environment.GetEnvironmentVariable("BUILD_REVISION");
        static string orgForeignKey = Environment.GetEnvironmentVariable("CORE_PROJECT_ID").Split("/")[0];
        static string projectGuid = Environment.GetEnvironmentVariable("CORE_PROJECT_ID").Split("/")[1];
        static string buildTarget = Environment.GetEnvironmentVariable("BUILD_TARGET");

        public static void PostBuild(string exportPath)
        {
            try
            {
                Console.WriteLine("====[ Variáveis de Ambiente - Build ]====");
                Console.WriteLine($"📦 Repo Name (PLASTIC_REPO)     : {repoName}");
                Console.WriteLine($"📦 Branch Name (SCM_BRANCH)     : {branchName}");
                Console.WriteLine($"🔢 Build Number (BUILD_REVISION)   : {buildNumber}");
                Console.WriteLine($"🏢 Org ForeignKey (CORE_PROJECT_ID/[0]) : {orgForeignKey}");
                Console.WriteLine($"🧩 Project GUID (CORE_PROJECT_ID/[1])  : {projectGuid}");
                Console.WriteLine($"📦 Build Target (BUILD_TARGET)     : {buildTarget}");
                Console.WriteLine("=========================================");

                string version = Application.version;

                SendDataToAWSLambda(version);

                //Etapa de validação
                List<string> issues = new List<string>();

                if (string.IsNullOrEmpty(buildNumber))
                    issues.Add("Build Number Not Set (BUILD_REVISION)");

                if (string.IsNullOrEmpty(version))
                    issues.Add("Version not set");

                if (string.IsNullOrEmpty(orgForeignKey))
                    issues.Add("Org Key not set (CORE_PROJECT_ID/[0])");

                //if (string.IsNullOrEmpty(repoName))
                //    issues.Add("Repo Name not set (PLASTIC_REPO)");

                if (string.IsNullOrEmpty(branchName))
                    issues.Add("Branch Name not set (SCM_BRANCH)");

                if (string.IsNullOrEmpty(buildTarget))
                    issues.Add("Build Target not set (BUILD_TARGET)");


                if (issues.Count > 0)
                {
                    AutomationHealthCheckClient.SendWarning(string.Join("\n", issues));
                }
                else
                {
                    AutomationHealthCheckClient.SendHealthy();
                }
            }
            catch (Exception ex)
            {
                AutomationHealthCheckClient.SendError(ex.Message, ex.StackTrace);
            }
        }

        static void SendDataToAWSLambda(string version)
        {
            var url = "https://s9ihlq2bij.execute-api.sa-east-1.amazonaws.com/Prod/register-build-complete";

            var body = new
            {
                buildNumber,
                version,
                orgForeignKey,
                projectGuid,
                repoName,
                branchName,
                buildTarget
            };

            string json = JsonConvert.SerializeObject(body);

            using var client = new HttpClient();
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine("📤 Enviando payload para versionMapping:");
            Console.WriteLine(json);

            var response = client.PostAsync(url, content).GetAwaiter().GetResult();
            string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Version mapping enviado com sucesso!");
            }
            else
            {
                throw new Exception($"❌ Falha ao enviar version mapping: {response.StatusCode} | " + responseBody);
            }
        }
    }
}
#endif

