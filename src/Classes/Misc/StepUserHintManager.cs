
using MousyHub.Models.Services;

namespace MousyHub.Classes.Misc
{
    public class StepUserHintManager
    {
        ProviderService provider;
        SettingsService settings;
        private const int CloseDelay = 4000;
        public StepUserHintManager(ProviderService providerService, SettingsService settingsService, bool IsMobile)
        {
            provider = providerService;
            settings = settingsService;
            if (providerService.Status || IsMobile)
            {
                HideComponent = true;
            }
            RefreshStepGuide();
        }
        public enum AIModuleType
        {
            LLM,
            RAG,
            KokoroTTS,
            Wizard,
        }
        public Dictionary<AIModuleType, bool> AIModulesStatus = new Dictionary<AIModuleType, bool>
        {
            { AIModuleType.LLM, false },
            { AIModuleType.RAG, false },
            { AIModuleType.KokoroTTS, false },
            { AIModuleType.Wizard, false },

        };
        public Dictionary<string, bool> StepGuide = new Dictionary<string, bool>();

        public Action? ChangeListEvent;

        public bool GuideVisible = true;
        public bool AIModulesVisible = false;
        public bool CloseWindow = false;
        public bool HideComponent = false;
        public void SwitchModule(AIModuleType type, bool value)
        {
            if (HideComponent)
                return;

            if (AIModulesStatus.ContainsKey(type))
            {
                AIModulesStatus[type] = value;
            }
            else
            {
                AIModulesStatus.Add(type, value);
            }
            ChangeListEvent?.Invoke();
        }

        public void ShowModules() 
        { 
             AIModulesVisible = true;
        }
        public async Task CloseAll()
        {
            if (HideComponent)
                return;

            await Task.Delay(CloseDelay);
            AIModulesVisible = false;
            CloseWindow = true;
            ChangeListEvent?.Invoke();
            await Task.Delay(CloseDelay);
            HideComponent = true;
        }
        public void CloseGuide()
        {
            GuideVisible = false;
        }

        public void RefreshStepGuide()
        {
            if (HideComponent)
                return;

            StepGuide = new Dictionary<string, bool>();

            // Add initial steps based on provider type
            switch (provider.SelectType.Key)
            {
                case ProviderService.APIType.Cloud:
                    StepGuide.Add("Enter the API address of your service", !string.IsNullOrEmpty(settings.User.CloudBasedConfig.BaseUrl));
                    StepGuide.Add("Enter your service API Key, if required", !string.IsNullOrEmpty(settings.User.CloudBasedConfig.APIKey));
                    StepGuide.Add("Load the list of LLM models from your service", provider.ModelList.Count!=0);
                    StepGuide.Add("Select an LLM model from the list", !string.IsNullOrEmpty(provider.SelectModel));
                    StepGuide.Add("Connect", provider.Status);
                    break;
                case ProviderService.APIType.KoboldCPP:
                    StepGuide.Add("Launch KoboldCPP and select a loaded model", true);
                    StepGuide.Add("Connect", provider.Status);
                    break;
                case ProviderService.APIType.Native:
                    StepGuide.Add("Install at least one LLM model in .gguf format",provider.ModelList.Count != 0);
                    StepGuide.Add("Select an LLM model from the list", !string.IsNullOrEmpty(settings.User.SelfInferenceConfig.ModelPath));
                    StepGuide.Add("Don't forget to specify launch parameters suitable for the model and your PC", true);
                    StepGuide.Add("Connect", provider.Status);
                    break;
                default:
                    break;
            }
            // Apply dependency rule: If a step is not completed, all following steps must also be incomplete
            var steps = StepGuide.ToList();
            for (int i = 1; i < steps.Count; i++)
            {
                if (!steps[i - 1].Value) // If previous step is false
                {
                    StepGuide[steps[i].Key] = false; // Set current step to false
                }
            }


    

            ChangeListEvent?.Invoke();
        }
    }
}
