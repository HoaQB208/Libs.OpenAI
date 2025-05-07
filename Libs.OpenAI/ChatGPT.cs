using Newtonsoft.Json;
using System.Text;

namespace Libs.OpenAI
{
    public class ChatGPT
    {
        string _apiKey, _chatModel, _searchModel;

        public ChatGPT(string apiKey, string chatModel = "gpt-3.5-turbo-16k", string searchModel = "gpt-4o-mini")
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
                string jsonPayload = JsonConvert.SerializeObject(requestbody);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var httpResponse = await new HttpClient().SendAsync(request);
                string responseContent = await httpResponse.Content.ReadAsStringAsync();
                dynamic jsonObject = JsonConvert.DeserializeObject(responseContent)!;
                return jsonObject["choices"][0]["message"]["content"];
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
                string jsonPayload = JsonConvert.SerializeObject(requestbody);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var httpResponse = await new HttpClient().SendAsync(request);
                string responseContent = await httpResponse.Content.ReadAsStringAsync();
                dynamic jsonObject = JsonConvert.DeserializeObject(responseContent)!;
                string result = jsonObject["output"][1]["content"][0]["text"];
                return result;
            }
            catch { }
            return "";
        }
    }
}