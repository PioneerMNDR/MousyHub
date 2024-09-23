using LLama.Common;
using MousyHub.Components.Abstractions;
using MousyHub.Components.Models.KoboldCPP;
using MousyHub.Components.Models.LLama;
using MousyHub.Components.Models.Misc;
using MousyHub.Components.Models.Misc.Audio;
using MousyHub.Components.Models.Model;
using MousyHub.Components.Models.User;

namespace MousyHub.Components.Models.Services
{
    public class ProviderService
    {


        public Dictionary<APIType, string> ConnectionsTypes = new Dictionary<APIType, string>
        {
            { APIType.Self_Inference,"Self-launch" },
            { APIType.KoboldCPP,"KoboldCPP" },
            { APIType.Chat_Completions,"Chat Completions API (soon...)" },

        };
        public KeyValuePair<APIType, string> SelectType = new KeyValuePair<APIType, string>();
        public enum APIType
        {
            Chat_Completions,
            KoboldCPP,
            Self_Inference
        }
        public ILanguageModel? LLModel;
        public Wizard Wizard { get; set; } = new Wizard();
        public STTHandler STT { get; set; } = new STTHandler();
        public string BaseUrl = "http://localhost:5001";
        public bool Status = false;
        private bool isLocalRun = false;
        public string MaxContextSize = "?";
        public bool WizardStatus = false;
        public delegate Task TaskBoolDelegate(bool status);
        public event TaskBoolDelegate ConnectionEvent;
        public event Action ConnectionChangeEvent;
        public ProviderService()
        {
            SelectType = ConnectionsTypes.First();
        }

        public async Task<string> NewConnect(SettingsService Settings)
        {
            if (Status && LLModel != null)
            {
                LLModel.Dispose();
            }

            switch (SelectType.Key)
            {
                case APIType.KoboldCPP:
                    bool IsSuccessK = await ConnectKoboldCPP();
                    if (!IsSuccessK)
                        return "";
                    await NewWizardConnect(Settings.CurrentInstruct,  Settings.User);
                    return await LLModel.Model();
                case APIType.Self_Inference:
                    bool IsSuccessL = await ConnectLocal(Settings);
                    if (!IsSuccessL)
                        return "";
                    await NewWizardConnect(Settings.CurrentInstruct, Settings.User);
                    return await LLModel.Model();
                case APIType.Chat_Completions:
                    break;
                default:
                    break;
            }
            await ConnectionEvent.Invoke(Status);

            return "";
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


        private async Task<bool> ConnectKoboldCPP()
        {
            LLModel = new KoboldProvider(BaseUrl);
            Status = await LLModel.Status();
            await ConnectionEvent.Invoke(Status);
            if (!Status)
            {
                return Status;
            }
            MaxContextSize = await MaxTokenCount();
            return Status;
        }
        private async Task<bool> ConnectLocal(SettingsService Settings)
        {
            if (Settings.User.SelfInferenceConfig.ModelPath==string.Empty)
            {
                return false;
            }
            ModelParams modelParams = new ModelParams(Settings.User.SelfInferenceConfig.ModelPath)
            {
                ContextSize = (uint)Settings.User.SelfInferenceConfig.ContextSize,
                UseMemoryLock = Settings.User.SelfInferenceConfig.UseMemoryLock,
                UseMemorymap = Settings.User.SelfInferenceConfig.UseMemorymap,
                GpuLayerCount = Settings.User.SelfInferenceConfig.GpuLayerCount,
                Threads = Settings.User.SelfInferenceConfig.Threads,
                BatchThreads = Settings.User.SelfInferenceConfig.BatchThreads,
                BatchSize = Settings.User.SelfInferenceConfig.BatchSize
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
        public async Task<string> NewWizardConnect(Instruct instruct, UserState userState)
        {
            if (Status)
            {
                Wizard.Run(LLModel, instruct, userState);
                WizardStatus = true;
                return "";
            }
            return "";
            //var Core = new LocalLlamaCore();
            //await Core.Run(modelParams);
            //var g = new LocalLLamaProvider(Core);
            //Wizard.Run(g, instruct, generationConfig);
            //WizardStatus = true;
            //return "";
        }

    }
}
