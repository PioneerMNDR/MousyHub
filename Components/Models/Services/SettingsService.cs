using LLMRP.Components.Models.Model;
using LLMRP.Components.Models.User;
using Theme = LLMRP.Components.Models.Misc.Theme;


namespace LLMRP.Components.Models.Services
{
    public class SettingsService
    {
        public List<GenerationConfig> PresetsList { get; set; }
        public List<Instruct> InstructList { get; set; }

        public List<Theme> ThemeList { get; set; }

        public List<string> LocalModelsList { get; set; }

        public List<Person> ProfileList { get; set; }

        public AlertServices alertServices;

        public UserState User;

        public SettingsService(UploaderService uploaderService, ProviderService providerServices, AlertServices alertServices)
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
            LocalModelsList = uploaderService.LoadModelsPath();
            if (LocalModelsList.Contains(User.SelfInferenceConfig.ModelPath) == false)
                User.SelfInferenceConfig.ModelPath = "";
            ProfileList = uploaderService.LoadProfileList();
            LoadDefault();
            Console.WriteLine("~This SettingsService is main service. Other connections is off ~");
            uploaderService.settingsService = this;
            this.alertServices = alertServices;
            Console.WriteLine("~Completing loading from memory");
        }


        private void LoadDefault()
        {
            //Main
            CurrentInstruct = LoadInstruct(DefaultName: "ChatML");
            CurrentGenerationConfig = LoadGenConfig(DefaultName: "simple-proxy-for-tavern", User.GenConfigName);
            GenConfigWizard = LoadGenConfig(DefaultName: "Deterministic", User.WizardGenConfigName);
            Theme = LoadTheme(DefaultName: "Default");
            //UserProfile
            if (User.ProfileName != null && User.ProfileName != "")
            {
                CurrentUserProfile = ProfileList.Where(x => x.Name == User.ProfileName).FirstOrDefault();
            }
        }



        public Instruct CurrentInstruct { get; set; }
        public GenerationConfig CurrentGenerationConfig { get; set; }
        public GenerationConfig GenConfigWizard { get; set; }

        public Theme Theme { get; set; }

        public Person? CurrentUserProfile { get; set; }


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


    }
}
