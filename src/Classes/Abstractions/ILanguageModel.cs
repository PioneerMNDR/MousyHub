using MousyHub.Classes.Model;
using MousyHub.Models.Model;

namespace MousyHub.Models.Abstractions

{
    public interface ILanguageModel : IDisposable
    {

        bool isBusy { get; }
        event EventHandler BusyChanged;
        Task<MessageResponse> GenerateTextAsync(Promt prompt, GenerationConfig config, int maxTokens = 100, int contextLength = 4096, string? stop_sequence = null, string key = "main");
        Task GenerateStreamTextAsync(Promt prompt, GenerationConfig config, int maxTokens = 100, int contextLength = 4096, string stop_sequence = "", string key = "main", Func<MessageResponse, Task> onTokenReceived = null);
        Task<bool> Status();
        Task<string> Model();
        Task<int> TokenCount(string promt);
        Task<int> MaxTokenCount();
        Task<string> GetChatTemplateRaw();
        Task Abort();
    }
}
