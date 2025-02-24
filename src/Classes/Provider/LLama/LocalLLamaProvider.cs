using LLama.Common;
using LLama.Sampling;
using MousyHub.Classes.Misc;
using MousyHub.Classes.Model;
using MousyHub.Models.Abstractions;
using MousyHub.Models.Model;
using MousyHub.Models.Provider.LLama.Sampler;


namespace MousyHub.Models.Provider.LLama
{
    public class LocalLLamaProvider : ILanguageModel
    {
        private readonly LocalLlamaCore _core;

        public LocalLLamaProvider(LocalLlamaCore core)
        {
            _core = core;
        }
        CancellationTokenSource _cts = new CancellationTokenSource();

        private bool _isbusy;

        public event EventHandler BusyChanged;

        public bool isBusy { get { return _isbusy; } private set { _isbusy = value; BusyChanged?.Invoke(this, EventArgs.Empty); } }

        public InferenceParams ConvertFromGenerationConfig(GenerationConfig config, int maxTokens = 100, string stop_seq = "")
        {
            Grammar grammar = null;
            ISamplingPipeline pipeline = null;
            if (config.grammar != null && config.grammar != "")
            {
                grammar = new Grammar(config.grammar, "root");
            }

            if (config.mirostat == 1)
            {
                pipeline = new Mirostat1Sampler
                {
                    Eta = (float)config.mirostat_eta,
                    Tau = (float)config.mirostat_tau,
                   
                };
            }
            if (config.mirostat == 2)
            {
                pipeline = new Mirostat2Sampler
                {
                    Eta = (float)config.mirostat_eta,
                    Tau = (float)config.mirostat_tau,
                };
            }
            else
            {
                pipeline = new BaseCustomSampler
                {
                    TopK = config.top_k,
                    TopP = (float)config.top_p,
                    MinP = (float)config.min_p,
                    TypicalP = (float)config.typical,
                    Temperature = (float)config.temp,
                    RepeatPenalty = (float)config.rep_pen,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0,
                    PenalizeNewline = true, // Default value 
                    RepeatPenaltyCount = config.rep_pen_range,
                    Grammar = grammar
                };

            }


            var inferenceParams = new InferenceParams
            {
                MaxTokens = maxTokens,
                SamplingPipeline = pipeline,

            };

            if (stop_seq != "")
            {
                inferenceParams.AntiPrompts = StringHelperBuilder.Stop_sequence_split(stop_seq).ToList();
            }

            return inferenceParams;
        }


        public Task Abort()
        {
            return _cts.CancelAsync();
        }

        public async Task GenerateStreamTextAsync(Promt prompt, GenerationConfig config, int maxTokens = 100, int contextLength = 4096, string stop_seq = "", string key = "main", Func<MessageResponse, Task> onTokenReceived = null)
        {
            isBusy = true;
            _cts = new CancellationTokenSource();
            InferenceParams inferenceParams = ConvertFromGenerationConfig(config, maxTokens, stop_seq);
            await _core.GenerateStream(prompt.FullContent, key, inferenceParams, onTokenReceived, _cts.Token);
            isBusy = false;
        }

        public async Task<MessageResponse> GenerateTextAsync(Promt prompt, GenerationConfig config, int maxTokens = 100, int contextLength = 4096, string stop_seq = "", string key = "main")
        {
            isBusy = true;
            _cts = new CancellationTokenSource();
            InferenceParams inferenceParams = ConvertFromGenerationConfig(config, maxTokens, stop_seq);
            var res = await _core.Generate(prompt.FullContent, key, inferenceParams, _cts.Token);
            isBusy = false;
            return res;

        }

        public Task<string> Model()
        {
            return _core.ModelInfo();
        }

        public Task<bool> Status()
        {
            return _core.Status();
        }

        public async Task<int> TokenCount(string promt)
        {
            return _core.TokenCount(promt);
        }

        public async Task<int> MaxTokenCount()
        {
            return _core.ContextSize();
        }

        public void Dispose()
        {
            _core.Dispose();
        }

        public async Task<string> GetChatTemplateRaw()
        {
          return await Task.Run(_core.GetModelChatTemplateRaw);
        }
    }
}
