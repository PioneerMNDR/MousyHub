using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Http;
using MousyHub.Classes.Model;
using MousyHub.Models.Model;
using MousyHub.Models.Services.URLHandle;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

public class ChatCompletionsClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ChatCompletionsClient(string baseUrl, string apiKey)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
        _apiKey = apiKey;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    /// <summary>
    /// Получает список доступных моделей.
    /// </summary>
    public async Task<string[]> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("v1/models", cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<OpenAIModelsResponse>(cancellationToken: cancellationToken);
        return content?.Data.Select(model => model.Id).ToArray() ?? Array.Empty<string>();
    }

    /// <summary>
    /// Отправляет запрос на генерацию текста.
    /// </summary>
    public async Task<MessageResponse> GenerateTextAsync(string model, Promt prompt, GenerationConfig config, int maxTokens = 100, string? stopSequence = null, CancellationToken cancellationToken = default)
    {
        var messages = prompt.Elements.Select(e => new { role = e.MessageRole.ToString().ToLower(), content = e.Content }).ToList();

        var request = new
        {
            model,
            messages,
            max_tokens = maxTokens,
            temperature = config.temp,
            top_p = config.top_p,
            stop = stopSequence,
            top_k = config.top_k,
            min_p = config.min_p,
            repeat_penalty = config.rep_pen,
        };

        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };
            response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            return new MessageResponse { IsSuccess = false, ErrorMessage = $"Request error: {ex.Message}" };
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            return new MessageResponse { IsSuccess = false, ErrorMessage = "The operation was canceled by the user." };
        }
        catch (Exception ex)
        {
            return new MessageResponse { IsSuccess = false, ErrorMessage = $"Unexpected error: {ex.Message}" };
        }

        try
        {
            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>(cancellationToken: cancellationToken);
            if (result == null)
            {
                return new MessageResponse { IsSuccess = false , ErrorMessage = "The API returned an empty response." };
            }

            return new MessageResponse { IsSuccess = true, Content = result.Choices[0].Message.Content };
        }
        catch (JsonException ex)
        {
            return new MessageResponse { IsSuccess = false, ErrorMessage = $"Failed to deserialize response: {ex.Message}" };
        }
    }

    /// <summary>
    /// Отправляет потоковый запрос на генерацию текста.
    /// </summary>
    public async Task GenerateStreamTextAsync(string model, Promt prompt, GenerationConfig config, Action<MessageResponse> onMessage, int maxTokens = 100, string stopSequence = "", Func<string, Task> onTokenReceived = null, CancellationToken cancellationToken = default)
    {
        var messages = prompt.Elements.Select(e => new { role = e.MessageRole.ToString().ToLower(), content = e.Content }).ToList();

        var request = new
        {
            model,
            messages,
            max_tokens = maxTokens,
            temperature = config.temp,
            top_p = config.top_p,
            stop = stopSequence,
            top_k = config.top_k,
            min_p = config.min_p,
            repeat_penalty = config.rep_pen,
            stream = true
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                onMessage(new MessageResponse { IsSuccess = false, ErrorMessage = "API returned an error: " + response.ReasonPhrase });
                return;
            }
        }
        catch (Exception ex)
        {
            onMessage(new MessageResponse { IsSuccess = false, ErrorMessage = "Connection error: " + ex.Message });
            return;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var json = line[6..].Trim();
                if (json == "[DONE]") break;

                try
                {
                    var chunk = JsonSerializer.Deserialize<OpenAIChatStreamResponse>(json);
                    var content = chunk?.Choices?[0]?.Delta?.Content;
                    if (!string.IsNullOrEmpty(content))
                    {
                        onMessage(new MessageResponse { IsSuccess = true, Content = content });
                    }
                }
                catch (Exception ex)
                {
                    onMessage(new MessageResponse { Content = "", IsSuccess = false, ErrorMessage = "API returned an error: " + ex.Message });
                }
            }
        }
    }

    /// <summary>
    /// Отправляет запрос для получения эмбеддингов.
    /// </summary>
    public async Task<float[]> GetEmbeddingsAsync(
        string model,
        string input,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model,
            input
        };

        var response = await _httpClient.PostAsJsonAsync(
            "v1/embeddings",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingsResponse>(cancellationToken: cancellationToken);
        return content?.Data[0].Embedding ?? Array.Empty<float>();
    }


    public async Task<bool> GetStatus()
    {
        if (_httpClient != null && _httpClient.BaseAddress != null)
        {
            var list = await URLImporterService.GetModelIdsAsync(_httpClient.BaseAddress.ToString(), _apiKey);
            if (list != null && list.Count > 0)
            {
                return true;
            }
        }
        return false;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Модели для десериализации ответов
public class OpenAIModelsResponse
{
    [JsonPropertyName("data")]
    public ModelData[] Data { get; set; }

    public class ModelData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}

public class OpenAIChatResponse
{
    [JsonPropertyName("choices")]
    public ChatChoice[] Choices { get; set; }

    public class ChatChoice
    {
        [JsonPropertyName("message")]
        public Message Message { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}

public class OpenAIChatStreamResponse
{
    [JsonPropertyName("choices")]
    public ChatStreamChoice[] Choices { get; set; }

    public class ChatStreamChoice
    {
        [JsonPropertyName("delta")]
        public Delta Delta { get; set; }
    }

    public class Delta
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}

public class OpenAIEmbeddingsResponse
{
    [JsonPropertyName("data")]
    public EmbeddingData[] Data { get; set; }

    public class EmbeddingData
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; }
    }
}
