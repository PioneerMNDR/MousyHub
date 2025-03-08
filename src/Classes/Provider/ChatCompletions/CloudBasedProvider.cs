using MousyHub.Classes.Model;
using MousyHub.Models.Abstractions;
using MousyHub.Models.Model;


public class ChatCompletionProvider : ILanguageModel
{
    private readonly ChatCompletionsClient _client;
    private readonly string _modelName;
    private bool _isBusy;
    CancellationTokenSource _cts = new CancellationTokenSource();
    public bool isBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                BusyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler BusyChanged;

    public ChatCompletionProvider(string baseUrl, string apiKey, string modelName)
    {
        _client = new ChatCompletionsClient(baseUrl, apiKey);
        _modelName = modelName;
    }

    public async Task<MessageResponse> GenerateTextAsync(Promt prompt, GenerationConfig config, int maxTokens = 100, int contextLength = 4096, string? stopSequence = null, string key = "main")
    {
        isBusy = true;
        _cts = new CancellationTokenSource();
        try
        {
            var response = await _client.GenerateTextAsync(_modelName, prompt, config, maxTokens, stopSequence, _cts.Token);

            if (response.IsSuccess)
                return new MessageResponse(response.Content, true, "");

            return new MessageResponse("", false, response.ErrorMessage);
        }
        catch (Exception ex)
        {
            return new MessageResponse("", false, ex.Message);
        }
        finally
        {
            isBusy = false;
        }
    }

    public async Task GenerateStreamTextAsync(Promt prompt, GenerationConfig config, int maxTokens = 100, int contextLength = 4096, string stopSequence = "", string key = "main", Func<MessageResponse, Task> onTokenReceived = null)
    {
        isBusy = true;
        _cts = new CancellationTokenSource();
        try
        {
            await _client.GenerateStreamTextAsync(_modelName, prompt, config, 
                async messageResponse => await onTokenReceived(new MessageResponse { IsSuccess = messageResponse.IsSuccess, ErrorMessage = messageResponse.ErrorMessage, Content = messageResponse.Content }), 
                maxTokens, stopSequence,cancellationToken: _cts.Token);
        }
        finally
        {
            isBusy = false;
        }
    }

    public Task Abort()
    {
        return _cts.CancelAsync();
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    // Остальные методы
    public async Task<string> GetChatTemplateRaw()
    {
        return "";
    }
    public async Task<int> MaxTokenCount()
    {
        return 0;
    }
    public async Task<string> Model()
    {
        return _modelName;
    }
    public async Task<bool> Status()
    {
        return await _client.GetStatus();
    }
    public async Task<int> TokenCount(string prompt)
    {
        return 0;
    }
}