using LLMRP.Components.Abstractions;
using LLMRP.Components.Models.Model;
using Newtonsoft.Json;

namespace LLMRP.Components.Models.KoboldCPP
{
    public class KoboldProvider : ILanguageModel
    {
        private readonly KoboldClient client;
        public KoboldProvider(string Url = "http://localhost:5001")
        {
            client = new KoboldClient(Url);
        }

        private bool _isbusy;

        public event EventHandler BusyChanged;

        public bool isBusy { get { return _isbusy; } private set { _isbusy = value; BusyChanged.Invoke(this, EventArgs.Empty); } }


        public async Task<MessageResponse> GenerateTextAsync(string prompt, GenerationConfig config, int maxTokens = 100, int contextLength = 4096, string stop_seq = "", string key = "main")
        {
            isBusy = true;
            string json = JsonConvert.SerializeObject(config);
            KoboldGenParams param = JsonConvert.DeserializeObject<KoboldGenParams>(json);
            param.Prompt = prompt;
            param.MaxLength = maxTokens;
            param.MaxContextLength = contextLength;
            param.stop_sequence = PromtBuilder.Stop_sequence_split(stop_seq);
            param.genkey = key;
            param.dry_sequence_breakers = PromtBuilder.Stop_sequence_split(config.dry_sequence_breakers_string);
            param.SetDynTemp(config.dynatemp, (float)config.min_temp, (float)config.max_temp);
            var output = await client.Generate(param);
            isBusy = false;
            if (output != null)
            {
                return new MessageResponse(output.Results[0].Text, true, "");
            }
            return new MessageResponse(null, false, "API error");

        }
        public async Task GenerateStreamTextAsync(string prompt, GenerationConfig config, int maxTokens = 100, int contextLength = 4096, string stop_seq = "", string key = "main", Func<MessageResponse, Task> onTokenReceived = null)
        {
            isBusy = true;
            string json = JsonConvert.SerializeObject(config);
            KoboldGenParams param = JsonConvert.DeserializeObject<KoboldGenParams>(json);
            param.Prompt = prompt;
            param.MaxLength = maxTokens;
            param.MaxContextLength = contextLength;
            param.stop_sequence = PromtBuilder.Stop_sequence_split(stop_seq);
            param.genkey = key;
            param.dry_sequence_breakers = PromtBuilder.Stop_sequence_split(config.dry_sequence_breakers_string);
            param.SetDynTemp(config.dynatemp, (float)config.min_temp, (float)config.max_temp);
            await client.GenerateStream(param, async messageResponse =>
            {
                var token = JsonConvert.DeserializeObject<Token>(messageResponse.Content);
                if (token == null)
                {
                    token = new Token()
                    {
                        token = ""
                    };
                }
                if (onTokenReceived != null)
                {

                    await onTokenReceived(new MessageResponse { IsSuccess = messageResponse.IsSuccess, ErrorMessage = messageResponse.ErrorMessage, Content = token.token });
                }
            });
            isBusy = false;
        }

        public async Task Abort()
        {
            await client.Abort();
        }

        public async Task<bool> Status()
        {
            return await client.Status();
        }

        public async Task<string> Model()
        {
            return await client.Model();
        }

        public async Task<int> TokenCount(string promt)
        {
            return await client.TokenCount(promt);
        }

        public async Task<int> MaxTokenCount()
        {
            return await client.ContextLength();
        }

        public void Dispose()
        {
            client.Dispose();
        }

        private class Token
        {
            public string token { get; set; }
        }

    }
}
