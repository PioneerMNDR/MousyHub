namespace MousyHub.Classes.Provider.ChatCompletions
{
    public class CloudBasedConfig
    {
        public string BaseUrl { get; set; } = "http://localhost:5001";
        public string APIKey { get; set; }
        public bool UseChatCompletions { get; set; } = false;
    }
}
