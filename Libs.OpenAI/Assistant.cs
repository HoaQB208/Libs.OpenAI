using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Libs.OpenAI
{
    public class Assistant
    {
        string _apiKey, _assistantId, _threadId;

        public Assistant(string apiKey, string assistantId, string threadId = null!)
        {
            _apiKey = apiKey;
            _assistantId = assistantId;
            _threadId = threadId;
        }

        public async Task<string> GetInstructions()
        {
            if (string.IsNullOrEmpty(_assistantId)) return "Error: AssistantId is empty!";
            try
            {
                using var httpClient = new HttpClient();
                var url = $"https://api.openai.com/v1/assistants/{_assistantId}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Headers.Add("OpenAI-Beta", "assistants=v2");
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode(); // Gây lỗi nếu HTTP code không thành công
                var content = await response.Content.ReadAsStringAsync();
                var jsonNode = JsonNode.Parse(content);
                string instructions = jsonNode?["instructions"]?.ToString()!;
                return instructions;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> SaveInstructions(string instructions)
        {
            if (string.IsNullOrEmpty(_assistantId)) return "Error: AssistantId is empty!";
            try
            {
                using var httpClient = new HttpClient();
                var url = $"https://api.openai.com/v1/assistants/{_assistantId}";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Headers.Add("OpenAI-Beta", "assistants=v2");
                var jsonBody = JsonSerializer.Serialize(new { instructions });
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode(); // Ném lỗi nếu status không thành công
                return "";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}