using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Libs.OpenAI
{
    public class ChatGPT
    {
        string _apiKey, _chatModel, _searchModel;

        public ChatGPT(string apiKey, string chatModel = "gpt-4o-mini", string searchModel = "gpt-4o-mini")
        {
            _apiKey = apiKey;
            _chatModel = chatModel;
            _searchModel = searchModel;
        }

        public async Task<string> Chat(string prompt)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                var requestbody = new
                {
                    model = _chatModel,
                    messages = new[] { new { role = "user", content = prompt } }
                };
                string jsonPayload = JsonSerializer.Serialize(requestbody);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var httpResponse = await new HttpClient().SendAsync(request);
                string responseContent = await httpResponse.Content.ReadAsStringAsync();
                JsonNode jsonObject = JsonNode.Parse(responseContent)!;
                return jsonObject["choices"]?[0]?["message"]?["content"]?.ToString() ?? "";
            }
            catch { }
            return "";
        }

        public async Task<string> Search(string prompt)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                var requestbody = new
                {
                    model = _searchModel,
                    tools = new[]
                    {
                        new { type = "web_search_preview" }
                    },
                    input = prompt
                };
                string jsonPayload = JsonSerializer.Serialize(requestbody);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var httpResponse = await new HttpClient().SendAsync(request);
                string responseContent = await httpResponse.Content.ReadAsStringAsync();
                JsonNode jsonObject = JsonNode.Parse(responseContent)!;
                string result = jsonObject["output"]?[1]?["content"]?[0]?["text"]?.ToString()!;
                return result;
            }
            catch { }
            return "";
        }
    }
}