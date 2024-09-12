using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Wordprocessing;
using LLama;
using LLama.Abstractions;
using LLama.Common;
using LLMRP.Components.Abstractions;
using LLMRP.Components.Models.LLama;
using LLMRP.Components.Models.Model;

namespace LLMRP.Components.Translator
{
    public class LocalLLMTranslatorBeta : ITranslator
    {
        public LocalLLMTranslatorBeta(ILanguageModel languageModel)
        {
            return;
            ModelParams modelParams = new ModelParams("./wwwroot/models/qwen2-1_5b-instruct-q6_k.gguf")
            {
                ContextSize = 2048,
                GpuLayerCount = 0,

            };
            var Core = new LocalLlamaCore();
            Core.Run(modelParams);
            LanguageModel = new LocalLLamaProvider(Core);

        }

        ILanguageModel LanguageModel { get; set; }


        public async Task<string> Translate(string text)
        {

            string promt = "<|im_start|>system\n Mode: Translation from English to Russian. Characters:Cyrillic only. Output: Only translated text. <|im_end|>\n<|im_start|>user\ntranslate to ru: " + text + "<|im_end|>\n<|im_start|>assistant\ntranslated text:";
            string response = "";
            GenerationConfig s = new GenerationConfig();
            s.temp = 0.2;
            s.top_p = 0;
            s.top_a = 0;
            s.typical = 1;
            s.tfs = 1;
            s.rep_pen = 1;
            s.top_k = 0;

            var g = await LanguageModel.GenerateTextAsync(promt, s, 300, 2048, key: "translator");
            response = g.Content;
            return response;

        }
    }
}
