using LLama.Common;
using MousyHub.Classes.Misc;
using MousyHub.Classes.Services.TTS;
using MousyHub.Models.Abstractions;
using MousyHub.Models.Misc;
using MousyHub.Models.Model;
using MousyHub.Models.Provider.KoboldCPP;
using MousyHub.Models.Provider.LLama;
using MousyHub.Models.User;


namespace MousyHub.Models.Services
{
    public class ProviderService
    {


        public Dictionary<APIType, string> ConnectionsTypes = new Dictionary<APIType, string>
        {
            { APIType.Native,"Native" },
            { APIType.KoboldCPP,"KoboldCPP" },
            { APIType.Cloud,"Cloud-based API" },

        };
        public KeyValuePair<APIType, string> SelectType = new KeyValuePair<APIType, string>();
        public enum APIType
        {
            Cloud,
            KoboldCPP,
            Native
        }
        public ILanguageModel? LLModel;
        public Wizard Wizard { get; set; } = new Wizard();


        public bool Status = false;
        private bool isLocalRun = false;
        public string MaxContextSize = "?";
        public bool WizardStatus = false;
        public delegate Task TaskBoolDelegate(bool status);
        public event TaskBoolDelegate ConnectionEvent;
        public event Action ConnectionChangeEvent;
        private UploaderService UploaderService;
        private KokoroService KokoroService;
        private RAGService RAG;
        //Chat completions options...
        public List<string> ModelList = new List<string>();
        public string SelectModel;
        public ProviderService(UploaderService uploaderService, RAGService RAG, KokoroService kokoroService)
        {
            SelectType = ConnectionsTypes.First();
            UploaderService = uploaderService;
            KokoroService = kokoroService;
            this.RAG = RAG;
        }

        public async Task<string> NewConnect(SettingsService Settings, StepUserHintManager hintManager)
        {
            if (Status && LLModel != null)
            {
                LLModel.Dispose();
            }
            bool WizardStatus = false;
            bool RAGStatus = false;
            bool KokoroStatus = false;
            bool LLMStatus = false;
            string ModelName = string.Empty;
            hintManager.ShowModules();
            switch (SelectType.Key)
            {
                case APIType.KoboldCPP:
                    LLMStatus = await ConnectKoboldCPP(Settings.User.KoboldURL);
                    if (!LLMStatus)
                        return "";
                    WizardStatus = await NewWizardConnect(Settings.CurrentInstruct, Settings.User);
                    RAGStatus = await TryRAGConnect(Settings.User.RAGOptions);
                    await TrySetAutoChatTemplate(Settings);
                    KokoroStatus = KokoroService.TryRunModel(UploaderService.LoadFirstKokoroModelPath());
                    ModelName = LLModel != null ? await LLModel.Model() : "";
                    break;
                case APIType.Native:
                    LLMStatus = await ConnectLocal(Settings);
                    if (!LLMStatus)
                        return "";
                    WizardStatus = await NewWizardConnect(Settings.CurrentInstruct, Settings.User);
                    RAGStatus = await TryRAGConnect(Settings.User.RAGOptions);
                    await TrySetAutoChatTemplate(Settings);
                    KokoroStatus = KokoroService.TryRunModel(UploaderService.LoadFirstKokoroModelPath());
                    ModelName = LLModel != null ? await LLModel.Model() : "";
                    break;
                case APIType.Cloud:
                    LLMStatus = await ConnectCloud(Settings.User.CloudBasedConfig.BaseUrl, Settings.User.CloudBasedConfig.APIKey);
                    if (!LLMStatus)
                        return "";
                    WizardStatus = await NewWizardConnect(Settings.CurrentInstruct, Settings.User);
                    RAGStatus = await TryRAGConnect(Settings.User.RAGOptions);
                    KokoroStatus = KokoroService.TryRunModel(UploaderService.LoadFirstKokoroModelPath());
                    break;
                default:
                    break;
            }
            await ConnectionEvent.Invoke(Status);
            hintManager.SwitchModule(StepUserHintManager.AIModuleType.LLM, LLMStatus);
            hintManager.SwitchModule(StepUserHintManager.AIModuleType.RAG, RAGStatus);
            hintManager.SwitchModule(StepUserHintManager.AIModuleType.KokoroTTS, KokoroStatus);
            hintManager.SwitchModule(StepUserHintManager.AIModuleType.Wizard, WizardStatus);
            if (LLMStatus)
            {
                hintManager.CloseGuide();
                _ = hintManager.CloseAll();
            }
            
            return ModelName;
        }

        public async Task CheckMainAPIStatus()
        {
            if (LLModel != null)
            {
                Status = await LLModel.Status();
                await ConnectionEvent.Invoke(Status);
            }

        }
        public void ChangeConnectionType()
        {
            ConnectionChangeEvent.Invoke();

        }
        public async Task<int> TokenCount(string promt)
        {
            if (Status)
            {
                return await LLModel.TokenCount(promt);
            }
            else
            {
                return (int)Math.Ceiling(promt.Length / 4.1);
            }
        }
        public async Task<string> MaxTokenCount()
        {
            if (Status)
            {
                var res = await LLModel.MaxTokenCount();
                return res.ToString();

            }
            return "?";
        }
        public async Task<bool> TrySetAutoChatTemplate(SettingsService settings)
        {
            if (Status)
            {
                var ct = await LLModel.GetChatTemplateRaw();
                var modelname = await LLModel.Model();
                var bestChatTemplate = ChatTemplateDetector.FindMatchingInstruct(ct, settings.InstructList);
                if (bestChatTemplate != null)
                {
                    settings.CurrentInstruct = bestChatTemplate;
                }
                if (settings.InstructList.Any(x => x.LinkedModels != null && x.LinkedModels.Contains(modelname)))
                {
                    var linkedInstruct = settings.InstructList.FirstOrDefault(x => x.LinkedModels != null && x.LinkedModels.Contains(modelname));
                    if (linkedInstruct != null)
                        settings.CurrentInstruct = linkedInstruct; Console.WriteLine("Choose linked chat template " + linkedInstruct.name);

                }
                return true;
            }
            return false;
        }


        private async Task<bool> ConnectKoboldCPP(string URL)
        {
            LLModel = new KoboldProvider(URL);
            Status = await LLModel.Status();
            await ConnectionEvent.Invoke(Status);
            if (!Status)
            {
                return Status;
            }
            MaxContextSize = await MaxTokenCount();
            return Status;
        }
        private async Task<bool> ConnectCloud(string URL, string APIKey)
        {
            if (string.IsNullOrEmpty(URL) || string.IsNullOrEmpty(SelectModel))
            {
                return false;
            }
            LLModel = new ChatCompletionProvider(URL, APIKey, SelectModel);
            Status = await LLModel.Status();
            await ConnectionEvent.Invoke(Status);
            if (!Status)
            {
                return Status;
            }
            return Status;
        }
        private async Task<bool> ConnectLocal(SettingsService Settings)
        {
            if (Settings.User.SelfInferenceConfig.ModelPath == string.Empty)
            {
                return false;
            }
            ModelParams modelParams = new ModelParams(Settings.User.SelfInferenceConfig.ModelPath)
            {
                ContextSize = (uint)Settings.User.SelfInferenceConfig.ContextSize,
                UseMemoryLock = Settings.User.SelfInferenceConfig.UseMemoryLock,
                UseMemorymap = Settings.User.SelfInferenceConfig.UseMemorymap,
                GpuLayerCount = Settings.User.SelfInferenceConfig.GpuLayerCount,
                Threads = (int?)Settings.User.SelfInferenceConfig.Threads,
                BatchThreads = (int?)Settings.User.SelfInferenceConfig.BatchThreads,
                BatchSize = Settings.User.SelfInferenceConfig.BatchSize,
                FlashAttention = Settings.User.SelfInferenceConfig.UseFlashAttention,

            };
            var Core = new LocalLlamaCore();
            var isSuccess = await Core.Run(modelParams);
            if (isSuccess)
            {
                LLModel = new LocalLLamaProvider(Core);
                Status = await LLModel.Status();
                isLocalRun = true;
                await ConnectionEvent.Invoke(Status);
                if (!Status)
                {
                    return Status;
                }
                MaxContextSize = await MaxTokenCount();
            }
            else
                Settings.alertServices.ErrorAlert("There is not enough memory to run the local model. Please reduce the number of GPU layers or the size of the context and try again.");

            return Status;


        }

        public async Task<bool> NewWizardConnect(Instruct instruct, UserState userState)
        {
            if (Status)
            {
                Wizard.UpdateInstructions(LLModel, instruct, userState, UploaderService, (SelectType.Key is APIType.Cloud && userState.CloudBasedConfig.UseChatCompletions) ? true : false);
                WizardStatus = true;
                return true;
            }
            return false;
        }
        public async Task<bool> TryRAGConnect(RAGOptions options)
        {
            if (options.Enabled)
            {
                string modelpath = UploaderService.LoadFirstEmbeddingModelPath();
                bool status = RAG.TryRun(modelpath);
                return status;
            }
            return false;
        }


    }
}
