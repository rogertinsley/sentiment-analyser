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
            // IMPORTANT!!! Since this webhook edits an issue comment, it needs to check
            // that the comment we're checking is a newly created comment and not an
            // edited comment or we could get into an infinite webhook loop.
            // Yes, this leaves a gap where a commenter can edit an innocuous comment
            // into an awful one and bypass this bot. Did I mention this is a proof of
            // concept?
            if (!action.Equals("created")) return Ok($"Ignored comment that was '{action}'");

            string comment    = data.comment.body;
            string issueTitle = data.issue.title;
            int repositoryId  = data.repository.id;
            int issueNumber   = data.issue.number;
            int commentId     = data.comment.id;

           // var sentimentScore = await AnalyzeSentiment(comment);
           var sentimentScore = MakeRequest();

            return Ok(sentimentScore);
        }

// https://westcentralus.dev.cognitive.microsoft.com/docs/services/TextAnalytics.V2.0/operations/56f30ceeeda5650db055a3c9/console
/**
{
  "documents": [
    {
      "language": "en",
      "id": "1234",
      "text": "hello world"
    }
  ]
}

 */
       private async static Task<string> MakeRequest()
        {
            var client          = new HttpClient();
            var queryString     = HttpUtility.ParseQueryString(string.Empty);
            var subscriptionKey = Environment.GetEnvironmentVariable("TEXT_ANALYTICS_API_KEY", EnvironmentVariableTarget.Process);
      
            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var uri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment?" + queryString;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("{\"documents\": [{\"language\": \"en\",\"id\": \"1234\",\"text\": \"hello world\"}]}");

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync(uri, content);

                return "";
            }
        }
    }
}