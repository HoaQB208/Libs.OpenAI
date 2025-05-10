using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Libs.OpenAI
{
    public class Assistant
    {
        string _apiKey, _assistantId;

        public Assistant(string apiKey, string assistantId)
        {
            _apiKey = apiKey;
            _assistantId = assistantId;
        }

        public async Task<string> Ask(string message, string threadId = null!)
        {
            if (string.IsNullOrEmpty(_assistantId)) return "Error: AssistantId is empty!";
            try
            {
                // Post Message
                string threadCreated = string.Empty;
                if (string.IsNullOrEmpty(threadId))
                {
                    var (id, error) = await CreateThreadAndPostMessage(message);
                    if (string.IsNullOrEmpty(id))
                    {
                        return $"Error: Ask Assistant >> CreateThreadAndPostMessage\n{error}";
                    }
                    else
                    {
                        threadId = id;
                        threadCreated = $"Thread created successfully, id={threadId}\n\n";
                    }
                }
                else
                {
                    string error = await PostMessage(threadId, message);
                    if (!string.IsNullOrEmpty(error))
                    {
                        return $"Error: Ask Assistant >> PostMessage\n{error}";
                    }
                }
                // Get Result
                string lastMsg = await GetLastMessage(threadId);
                return $"{threadCreated}{lastMsg}";
            }
            catch (Exception ex)
            {
                return $"Error: Ask Assistant\n{ex.Message}";
            }
        }

        /// <summary>
        /// CreateThreadAndPostMessage
        /// </summary>
        /// <param name="message"></param>
        /// <returns>(threadId, string)</returns>
        private async Task<(string, string)> CreateThreadAndPostMessage(string message)
        {
            if (string.IsNullOrEmpty(_assistantId))
            {
                return ("", "Error: AssistantId is empty!");
            }
            if (string.IsNullOrEmpty(message))
            {
                return ("", "Error: Message is empty!");
            }
            try
            {
                using var httpClient = new HttpClient();
                var url = $"https://api.openai.com/v1/threads/runs";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Headers.Add("OpenAI-Beta", "assistants=v2");
                string jsonBody = @"{
                                 ""assistant_id"": """ + _assistantId + @""",
                                 ""thread"": {
                                    ""messages"": [
                                        {
                                            ""role"": ""user"",
                                            ""content"": """ + message + @"""
                                        }
                                     ]
                                   }
                                 }";

                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode(); // Ném lỗi nếu status không thành công
                var content = await response.Content.ReadAsStringAsync();
                var jsonNode = JsonNode.Parse(content);
                return (jsonNode?["thread_id"]?.ToString() ?? "", "");
            }
            catch (Exception ex)
            {
                return ("", ex.Message);
            }
        }


        private async Task<string> PostMessage(string threadId, string message)
        {
            if (string.IsNullOrEmpty(_assistantId)) return "Error: AssistantId is empty!";
            if (string.IsNullOrEmpty(threadId)) return "Error: ThreadId is empty!";
            if (string.IsNullOrEmpty(message)) return "Error: Message is empty!";
            try
            {
                // PostMessage
                using var httpClient = new HttpClient();
                var url = $"https://api.openai.com/v1/threads/{threadId}/messages";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Headers.Add("OpenAI-Beta", "assistants=v2");
                string jsonBody = $@"
                                    {{
                                        ""role"": ""user"",
                                        ""content"": ""{message}""
                                    }}";
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode(); // Ném lỗi nếu status không thành công
                // Thực thi
                await Run(threadId);
                return "";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private async Task<string> Run(string threadId)
        {
            if (string.IsNullOrEmpty(_assistantId)) return "Error: AssistantId is empty!";
            if (string.IsNullOrEmpty(threadId)) return "Error: ThreadId is empty!";
            try
            {
                using var httpClient = new HttpClient();
                var url = $"https://api.openai.com/v1/threads/{threadId}/runs";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Headers.Add("OpenAI-Beta", "assistants=v2");
                string jsonBody = @"{ ""assistant_id"": """ + _assistantId + @"""  }";
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

        public async Task<string> GetLastMessage(string threadId)
        {
            if (string.IsNullOrEmpty(_assistantId)) return "Error: AssistantId is empty!";
            try
            {
                int count = 0;
                do
                {
                    try
                    {
                        using var httpClient = new HttpClient();
                        var url = $"https://api.openai.com/v1/threads/{threadId}/messages";
                        using var request = new HttpRequestMessage(HttpMethod.Get, url);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                        request.Headers.Add("OpenAI-Beta", "assistants=v2");
                        var response = await httpClient.SendAsync(request);
                        response.EnsureSuccessStatusCode(); // Gây lỗi nếu HTTP code không thành công
                        var content = await response.Content.ReadAsStringAsync();
                        var jsonNode = JsonNode.Parse(content);
                        var firstMessage = jsonNode?["data"]?[0];
                        if (firstMessage?["role"]?.ToString() == "assistant")
                        {
                            return firstMessage["content"]?[0]?["text"]?["value"]?.ToString() ?? "";
                        }
                    }
                    catch { }
                    await Task.Delay(200);

                    count++;
                } while (count < 50);

                return "";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
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
                return jsonNode?["instructions"]?.ToString() ?? "";
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