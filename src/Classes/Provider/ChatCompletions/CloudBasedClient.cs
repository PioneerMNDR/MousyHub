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
        string endpoint = prompt.isChat ? "v1/chat/completions" : "v1/completions";
        object requestBody;

        if (prompt.isChat)
        {
            var messages = prompt.Elements.Select(e => new { role = e.MessageRole.ToString().ToLower(), content = e.Content }).ToList();
            requestBody = new
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
        }
        else
        {
            // For non-chat completions, we use the FullContent property
            requestBody = new
            {
                model,
                prompt = prompt.FullContent,
                max_tokens = maxTokens,
                temperature = config.temp,
                top_p = config.top_p,
                stop = stopSequence,
                top_k = config.top_k,
                min_p = config.min_p,
                repeat_penalty = config.rep_pen,
            };
        }

        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
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
            if (prompt.isChat)
            {
                var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>(cancellationToken: cancellationToken);
                if (result == null || result.Choices == null || result.Choices.Length == 0)
                {
                    return new MessageResponse { IsSuccess = false, ErrorMessage = "The API returned an empty response." };
                }
                return new MessageResponse { IsSuccess = true, Content = result.Choices[0].Message.Content };
            }
            else
            {
                var result = await response.Content.ReadFromJsonAsync<OpenAICompletionResponse>(cancellationToken: cancellationToken);
                if (result == null || result.Choices == null || result.Choices.Length == 0)
                {
                    return new MessageResponse { IsSuccess = false, ErrorMessage = "The API returned an empty response." };
                }
                return new MessageResponse { IsSuccess = true, Content = result.Choices[0].Text };
            }
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
        string endpoint = prompt.isChat ? "v1/chat/completions" : "v1/completions";
        object requestBody;

        if (prompt.isChat)
        {
            var messages = prompt.Elements.Select(e => new { role = e.MessageRole.ToString().ToLower(), content = e.Content }).ToList();
            requestBody = new
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
        }
        else
        {
            requestBody = new
            {
                model,
                prompt = prompt.FullContent,
                max_tokens = maxTokens,
                temperature = config.temp,
                top_p = config.top_p,
                stop = stopSequence,
                top_k = config.top_k,
                min_p = config.min_p,
                repeat_penalty = config.rep_pen,
                stream = true
            };
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
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
                    if (prompt.isChat)
                    {
                        var chunk = JsonSerializer.Deserialize<OpenAIChatStreamResponse>(json);
                        var content = chunk?.Choices?[0]?.Delta?.Content;
                        if (!string.IsNullOrEmpty(content))
                        {
                            onMessage(new MessageResponse { IsSuccess = true, Content = content });
                            if (onTokenReceived != null)
                                await onTokenReceived(content);
                        }
                    }
                    else
                    {
                        var chunk = JsonSerializer.Deserialize<OpenAICompletionStreamResponse>(json);
                        var content = chunk?.Choices?[0]?.Text;
                        if (!string.IsNullOrEmpty(content))
                        {
                            onMessage(new MessageResponse { IsSuccess = true, Content = content });
                            if (onTokenReceived != null)
                                if (onTokenReceived != null)
                                    await onTokenReceived(content);
                        }
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

// Base response class
public class OpenAIResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}

// For non-chat completions
public class OpenAICompletionResponse : OpenAIResponse
{
    [JsonPropertyName("choices")]
    public CompletionChoice[] Choices { get; set; }
}

// For chat completions
public class OpenAIChatResponse : OpenAIResponse
{
    [JsonPropertyName("choices")]
    public ChatChoice[] Choices { get; set; }
}

// Choice for non-chat completions
public class CompletionChoice
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}

// Choice for chat completions
public class ChatChoice
{
    [JsonPropertyName("message")]
    public Message Message { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}

public class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}

// For streaming non-chat completions
public class OpenAICompletionStreamResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("object")]
    public string Object { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("choices")]
    public CompletionStreamChoice[] Choices { get; set; }
}

// For streaming chat completions
public class OpenAIChatStreamResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("object")]
    public string Object { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("choices")]
    public ChatStreamChoice[] Choices { get; set; }
}

public class CompletionStreamChoice
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}

public class ChatStreamChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("delta")]
    public Delta Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}

public class Delta
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}





// Модели для десериализации ответов
// Универсальный интерфейс для получения контента из разных типов ответов
public interface IContentProvider
{
    string GetContent();
}

// Базовый класс для ответов с выбором
public class OpenAIChoiceResponse : OpenAIResponse
{
    [JsonPropertyName("choices")]
    public object[] Choices { get; set; }

    // Метод для получения контента в зависимости от типа ответа
    public string GetContent()
    {
        if (Choices == null || Choices.Length == 0)
            return null;

        if (Choices[0] is IContentProvider provider)
            return provider.GetContent();

        return null;
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
