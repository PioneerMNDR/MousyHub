using MousyHub.Classes.Misc;
using MousyHub.Models;
using MousyHub.Models.Model;
using MousyHub.Models.User;
using System.Linq;
using Theme = MousyHub.Models.Misc.Theme;


namespace MousyHub.Models.Services
{
    public class SettingsService
    {
        public List<GenerationConfig> PresetsList { get; set; }
        public List<Instruct> InstructList { get; set; }
        public List<Theme> ThemeList { get; set; }
        public List<string> LocalModelsList { get; set; }
        public List<Person> ProfileList { get; set; }

        public AlertServices alertServices;
        //This class is responsible for user settings, which is serialized and stored in memory.
        public UserState User;
        private readonly ProviderService _providerServices;

        public SettingsService(UploaderService uploaderService, ProviderService providerServices, AlertServices alertServices, DiagnosticsService diagnostics)
        {
            //this.alertServices = alertServices;  
            while (uploaderService.isBusy)
            {
                Thread.Sleep(100);
            }
            Console.WriteLine("Loading from memory~");
            InstructList = uploaderService.LoadInstructs();
            PresetsList = uploaderService.LoadPresets();
            ThemeList = uploaderService.LoadThemes();
            User = uploaderService.LoadSettings();

            if (User.SelfInferenceConfig.Threads == null)
            {
                User.SelfInferenceConfig.Threads = (uint?)(diagnostics.LogicalProcessorCount / 2);
                User.SelfInferenceConfig.BatchThreads = (uint?)(diagnostics.LogicalProcessorCount / 2);
            }
            ProfileList = uploaderService.LoadProfileList();
            LoadModelsList(uploaderService);
            LoadDefault();
            uploaderService.settingsService = this;
            this.alertServices = alertServices;
            _providerServices = providerServices;
            Console.Beep();
        }


        private void LoadDefault()
        {
            //if current parameters is null
            //Main
            CurrentInstruct = LoadInstruct(DefaultName: "ChatML");
            CurrentGenerationConfig = LoadGenConfig(DefaultName: "simple-proxy-for-tavern", User.GenConfigName);
            Theme = LoadTheme(DefaultName: "Default");
            //UserProfile
            if (User.ProfileName != null && User.ProfileName != "")
            {
                CurrentUserProfile = ProfileList.Where(x => x.Name == User.ProfileName).FirstOrDefault();
            }
        }



        public Instruct CurrentInstruct { get; set; }
        public GenerationConfig CurrentGenerationConfig { get; set; }
        public Theme Theme { get; set; }

        public Person? CurrentUserProfile { get; set; }

        public void LoadModelsList(UploaderService uploader)
        {
            LocalModelsList = uploader.LoadModelsPath(User.CustomModelPathFolder);
            if (LocalModelsList.Contains(User.SelfInferenceConfig.ModelPath) == false)
                User.SelfInferenceConfig.ModelPath = LocalModelsList.FirstOrDefault("");
        }
        public void DeleteInstructPreset()
        {
            if (InstructList.Count >= 2)
            {
                InstructList.Remove(CurrentInstruct);
                alertServices.SuccessAlert(CurrentInstruct.name + " deleted");
                CurrentInstruct = InstructList.FirstOrDefault();
            }
        }
        public void DeleteGenConfigPreset()
        {
            if (PresetsList.Count >= 2)
            {
                PresetsList.Remove(CurrentGenerationConfig);
                alertServices.SuccessAlert(CurrentGenerationConfig.ConfigName + " deleted");
                CurrentGenerationConfig = PresetsList.FirstOrDefault();
            }
        }
        public void DeleteProfile()
        {
            if (ProfileList.Count >= 2)
            {
                ProfileList.Remove(CurrentUserProfile);
                alertServices.SuccessAlert(CurrentUserProfile.Name + " deleted");
                CurrentUserProfile = ProfileList.FirstOrDefault();
            }
        }

        Instruct LoadInstruct(string DefaultName)
        {
            if (InstructList.Count > 0)
            {
                var DefaultInstruct = InstructList.Where(x => x.name == DefaultName).FirstOrDefault(InstructList.First());
                return InstructList.Where(x => x.name == User.InstructName).FirstOrDefault(DefaultInstruct);
            }
            else
                return new Instruct();
        }
        GenerationConfig LoadGenConfig(string DefaultName, string UserConfigName)
        {
            if (PresetsList.Count > 0)
            {
                var DefaultConfig = PresetsList.Where(x => x.ConfigName == DefaultName).FirstOrDefault(PresetsList.First());
                return PresetsList.Where(x => x.ConfigName == UserConfigName).FirstOrDefault(DefaultConfig);
            }
            else
                return new GenerationConfig();
        }
        Theme LoadTheme(string DefaultName)
        {
            Theme theme = new Theme();
            theme.Name = DefaultName;
            theme.MudTheme = new MudBlazor.MudTheme();
            var loadTheme = ThemeList.Where(x => x.Name == User.ThemeName).FirstOrDefault(ThemeList.Where(x => x.Name == DefaultName).FirstOrDefault(theme));
            return loadTheme;
        }
        public async Task LinkModelToCurrentInstruct()
        {
            if (CurrentInstruct.LinkedModels == null)
                CurrentInstruct.LinkedModels = new List<string>();
            if (_providerServices.Status)
            {
                string modelname = await _providerServices.LLModel.Model();
                //we delete this model from all lists and bind it to the last selected one
                foreach (var item in InstructList.Where(x => x.LinkedModels != null && x.LinkedModels.Contains(modelname)))
                {
                    item.LinkedModels.Remove(modelname);
                }
                if (!CurrentInstruct.LinkedModels.Contains(modelname))
                    CurrentInstruct.LinkedModels.Add(modelname);
            }

        }


    }
}
