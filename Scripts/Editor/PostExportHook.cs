#if UNITY_EDITOR
using Codice.Utils;
using System;
using System.Net.Http;
using System.Text;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace YellowPanda.CloudBuild
{
    public class PostExportHook
    {
        static string repoName = HttpUtility.UrlEncode(Environment.GetEnvironmentVariable("PLASTIC_REPO"));
        static string branchName = HttpUtility.UrlEncode(Environment.GetEnvironmentVariable("SCM_BRANCH"));
        static string buildNumber = Environment.GetEnvironmentVariable("BUILD_REVISION");
        static string orgForeignKey = Environment.GetEnvironmentVariable("CORE_PROJECT_ID").Split("/")[0];
        static string projectGuid = Environment.GetEnvironmentVariable("CORE_PROJECT_ID").Split("/")[1];

        public static void PostBuild(string exportPath)
        {
            Console.WriteLine("====[ Variáveis de Ambiente - Build ]====");
            Console.WriteLine($"📦 Repo Name (PLASTIC_REPO)     : {repoName}");
            Console.WriteLine($"📦 Branch Name (SCM_BRANCH)     : {branchName}");
            Console.WriteLine($"🔢 Build Number (BUILD_REVISION)   : {buildNumber}");
            Console.WriteLine($"🏢 Org ForeignKey (CORE_PROJECT_ID/[0]) : {orgForeignKey}");
            Console.WriteLine($"🧩 Project GUID (CORE_PROJECT_ID/[1])  : {projectGuid}");
            Console.WriteLine("=========================================");

            string version = Application.version;
            SendDataToAWSLambda(version, repoName);
        }

        static void SendDataToAWSLambda(string version, string projectName)
        {
            var url = "https://plwyuwqm28.execute-api.sa-east-1.amazonaws.com/versionMapping";

            var body = new
            {
                buildNumber,
                version,
                orgForeignKey,
                projectGuid,
                projectName,
                repoName,
                branchName
            };

            string json = JsonConvert.SerializeObject(body);

            using var client = new HttpClient();
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine("📤 Enviando payload para versionMapping:");
            Console.WriteLine(json);

            try
            {
                var response = client.PostAsync(url, content).GetAwaiter().GetResult();
                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Version mapping enviado com sucesso!");
                }
                else
                {
                    Console.WriteLine($"❌ Falha ao enviar version mapping: {response.StatusCode}");
                    Console.WriteLine(responseBody);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Erro ao enviar versão: " + ex.Message);
            }
        }
    }
}
#endif
