using LLama.Common;
using LLMRP.Components.Abstractions;
using LLMRP.Components.Models.Model;
using Grammar = LLama.Grammars.Grammar;

namespace LLMRP.Components.Models.LLama
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
            if (config.grammar != null && config.grammar != "")
            {
                grammar = Grammar.Parse(config.grammar, "root");
            }


            var inferenceParams = new InferenceParams
            {
                // Assign values from GenerationConfig where possible
                RepeatPenalty = (float)config.rep_pen,
                MaxTokens = maxTokens,
                TopK = config.top_k,
                TfsZ = (float)config.tfs,
                TopP = (float)config.top_p,
                MinP = (float)config.min_p,
                TypicalP = (float)config.typical,
                Temperature = (float)config.temp,
                RepeatLastTokensCount = config.rep_pen_range,
                FrequencyPenalty = 0, // Assuming FrequencyPenalty is not used in GenerationConfig
                PresencePenalty = 0, // Assuming PresencePenalty is not used in GenerationConfig
                Mirostat = (MirostatType)config.mirostat,
                MirostatTau = (float)config.mirostat_tau,
                MirostatEta = (float)config.mirostat_eta,
                PenalizeNL = true, // Default value


            };
            if (grammar != null)
            {
                inferenceParams.Grammar = grammar.CreateInstance();

            }


            if (stop_seq != "")
            {
                inferenceParams.AntiPrompts = PromtBuilder.Stop_sequence_split(stop_seq).ToList();
            }


            // * SamplingPipeline: Requires understanding of sampler_order and potentially custom logic.

            return inferenceParams;
        }


        public Task Abort()
        {
            return _cts.CancelAsync();
        }

        public async Task GenerateStreamTextAsync(string prompt, GenerationConfig config, int maxTokens = 100, int contextLength = 4096, string stop_seq = "", string key = "main", Func<MessageResponse, Task> onTokenReceived = null)
        {
            isBusy = true;
            _cts = new CancellationTokenSource();
            InferenceParams inferenceParams = ConvertFromGenerationConfig(config, maxTokens, stop_seq);
            await _core.GenerateStream(prompt, key, inferenceParams, onTokenReceived, _cts.Token);
            isBusy = false;
        }

        public async Task<MessageResponse> GenerateTextAsync(string prompt, GenerationConfig config, int maxTokens = 100, int contextLength = 4096, string stop_seq = "", string key = "main")
        {
            isBusy = true;
            _cts = new CancellationTokenSource();
            InferenceParams inferenceParams = ConvertFromGenerationConfig(config, maxTokens, stop_seq);
            var res = await _core.Generate(prompt, key, inferenceParams, _cts.Token);
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
    }
}
