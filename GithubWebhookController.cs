using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sentiment
{
    [Route("webhooks/github")]
    public class GithubWebhookController : Controller
    {
        private readonly ILogger<GithubWebhookController> _logger;

        public GithubWebhookController(IConfiguration configuration, ILogger<GithubWebhookController> logger)
        {
            _logger = logger;
        }

        [HttpPost("")]
        public async Task<IActionResult> Post()
        {
            var payload = Request.Form["payload"];
            _logger.LogInformation(payload);
            dynamic data = JValue.Parse(payload);

            string action = data.action;
            if (!action.Equals("created")) return Ok($"Ignored comment that was '{action}'");

            string comment    = data.comment.body;
            string issueTitle = data.issue.title;
            int repositoryId  = data.repository.id;
            int issueNumber   = data.issue.number;
            int commentId     = data.comment.id;

            var sentimentScore = await AnalyzeSentiment(comment);
            var sentimentPercentage = Convert.ToDouble(sentimentScore);

            var sentimentComment = $"[Generated Comment] That comment received a sentiment score of {Math.Round(sentimentPercentage, 2)}%.";

            return Ok(sentimentComment);
        }

       private async Task<string> AnalyzeSentiment(string comment)
        {
            var handler         = new HttpClientHandler();
            handler.UseProxy    = true;
            handler.Proxy       = new WebProxy();
            var client          = new HttpClient(handler);

            var subscriptionKey = Environment.GetEnvironmentVariable("TEXT_ANALYTICS_API_KEY", EnvironmentVariableTarget.Process);
      
            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("{\"documents\": [{\"language\": \"en\",\"id\": \"1\",\"text\": \" " + comment + " \"}]}");

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync("https://westcentralus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment", content);

                var result = response
                                .EnsureSuccessStatusCode()
                                .Content
                                .ReadAsStringAsync()
                                .Result;

                JObject data = JObject.Parse(result);

                _logger.LogInformation($"Comment: {comment}");
                _logger.LogInformation($"Sentiment: {data["documents"][0]["score"].ToString()}");

                return data["documents"][0]["score"].ToString();
            }
        }
    }
}