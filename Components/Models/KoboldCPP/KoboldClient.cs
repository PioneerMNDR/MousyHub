using LLMRP.Components.Models.Model;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LLMRP.Components.Models.KoboldCPP
{
    public class KoboldClient : IDisposable
    {
        private readonly HttpClient _client;
        private readonly string _baseUri;

        public KoboldClient(string baseUri)
        {
            _client = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            _baseUri = baseUri;
        }

        public KoboldClient(string baseUri, HttpClient client)
        {
            _client = client;

            _baseUri = baseUri;
        }

        public async Task<ModelOutput?> Generate(KoboldGenParams parameters)
        {
            try
            {
                var payload = new StringContent(parameters.GetJson(), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync($"{_baseUri}/api/v1/generate", payload);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                content = content.Trim();
                var r = JsonSerializer.Deserialize<ModelOutput>(content);
                return r;
            }
            catch (Exception)
            {
                return null;
                throw;
            }


        }
        public async Task GenerateStream(KoboldGenParams parameters, Action<MessageResponse> onMessage)
        {
            var payload = new StringContent(parameters.GetJson(), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUri}/api/extra/generate/stream")
            {
                Content = payload
            };

            HttpResponseMessage response;
            try
            {
                response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            }
            catch (Exception ex)
            {
                onMessage(new MessageResponse { IsSuccess = false, ErrorMessage = "Connection error: " + ex.Message });
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                onMessage(new MessageResponse { IsSuccess = false, ErrorMessage = "API returned an error: " + response.ReasonPhrase });
                return;
            }

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream))
            {
                try
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line.StartsWith("data:"))
                        {
                            var data = line.Substring("data:".Length).Trim();
                            if (!string.IsNullOrEmpty(data))
                            {
                                onMessage(new MessageResponse { IsSuccess = true, Content = data });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                    onMessage(new MessageResponse { Content = "", IsSuccess = false, ErrorMessage = "API returned an error: " + ex.Message });
                }

            }
        }
        public async Task<ModelOutput> Check()
        {
            var payload = new StringContent(string.Empty);
            var response = await _client.PostAsync($"{_baseUri}/api/extra/generate/check", payload);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            content = content.Trim();
            return JsonSerializer.Deserialize<ModelOutput>(content);

        }

        public async Task Abort()
        {
            var payload = new StringContent(string.Empty);
            var response = await _client.PostAsync($"{_baseUri}/api/extra/abort", payload);
            await response.Content.ReadAsStringAsync();
        }

        public async Task<string> Version()
        {
            try
            {
                var response = await _client.GetAsync($"{_baseUri}/api/v1/info/version");
                var content = response.Content.ReadAsStringAsync().Result;
                var json = JsonDocument.Parse(content);
                return json.RootElement.GetProperty("result").GetString();
            }
            catch (Exception)
            {
                return "";
                throw;
            }
        }
        public async Task<bool> Status()
        {
            try
            {
                var response = await _client.GetAsync($"{_baseUri}/api/v1/info/version");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
                throw;
            }
        }

        public class TokenCountRequest()
        {
            [JsonPropertyName("prompt")]
            public string promt { get; set; } = string.Empty;
        }

        public async Task<int> TokenCount(string Promt)
        {
            try
            {
                var json = (string)System.Text.Json.JsonSerializer.Serialize(new TokenCountRequest() { promt = Promt });
                var payload = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync($"{_baseUri}/api/extra/tokencount", payload);
                response.EnsureSuccessStatusCode();
                var content = response.Content.ReadAsStringAsync().Result;
                var js = JsonDocument.Parse(content);
                return js.RootElement.GetProperty("value").GetInt32();
            }
            catch (Exception)
            {
                return 0;
                throw;
            }
        }
        public async Task<int> ContextLength()
        {
            try
            {
                var response = await _client.GetAsync($"{_baseUri}/api/v1/config/max_context_length");
                var content = response.Content.ReadAsStringAsync().Result;
                var json = JsonDocument.Parse(content);
                return json.RootElement.GetProperty("value").GetInt32();
            }
            catch (Exception)
            {
                return 0;
                throw;
            }
        }
        public async Task<string> Model()
        {
            try
            {
                var response = await _client.GetAsync($"{_baseUri}/api/v1/model");
                var content = response.Content.ReadAsStringAsync().Result;
                var json = JsonDocument.Parse(content);
                return json.RootElement.GetProperty("result").GetString();
            }
            catch (HttpRequestException)
            {
                return "";
                throw;
            }


        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
