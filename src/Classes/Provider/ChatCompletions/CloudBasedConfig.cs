namespace MousyHub.Classes.Provider.ChatCompletions
{
    public class CloudBasedConfig
    {
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/";
        public string APIKey { get; set; }
        public bool UseChatCompletions { get; set; } = false;
    }
}
