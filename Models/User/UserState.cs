using MousyHub.Models;
using MousyHub.Models.Misc;
using MousyHub.Models.Provider.LLama;
using MousyHub.Models.Services;

namespace MousyHub.Models.User
{
    public class UserState
    {
        public string GenConfigName { get; set; }
        public string InstructName { get; set; }
        public string ThemeName { get; set; }
        public string WizardGenConfigName { get; set; }
        public string ProfileName { get; set; }
        public LocalLLamaLaunchConfig SelfInferenceConfig { get; set; } = new LocalLLamaLaunchConfig();
        public TranslatorOptions TranslatorOptions { get; set; } = new TranslatorOptions();
        public RAGOptions RAGOptions { get; set; } =  new RAGOptions();
        public int CurrentMaxToken { get; set; } = 100;
        public int CurrentContextLength { get; set; } = 4096;
        public bool PauseBeforeGenerating { get; set; } = false;
        public bool AutoSummarize { get; set; } = true;

        public int SummarizeMessageCount = 10;
        public bool AnswerAssistant { get; set; } = false;
        public bool SquareAvatars { get; set; } = false;
        public bool HideNSFWPicture { get; set; } = false;

        public bool HideDownloadPage { get; set; } = false;
        private bool AttemptHideDownloadPage { get; set; } = false;
        public bool GenerateShortDesOnImport { get; set; } = true;
        public QuickReplySetting[] QuickRepliesSetings { get; set; } = { new QuickReplySetting(QuickReplySetting.ResponseEmotion.Positive), new QuickReplySetting(QuickReplySetting.ResponseEmotion.Neutral), new QuickReplySetting(QuickReplySetting.ResponseEmotion.Negative) };

        public bool isTutorialGone { get; set; } = false;
        private DateTime RegisterDate { get; set; } = DateTime.Now;

        public void SaveSettings(SettingsService settings)
        {
            //I turn off the Download tab three days after registration
            if (RegisterDate.AddDays(3) < DateTime.Now && AttemptHideDownloadPage == false)
            {
                AttemptHideDownloadPage = true;
                HideDownloadPage = true;
            }
            GenConfigName = settings.CurrentGenerationConfig.ConfigName;
            InstructName = settings.CurrentInstruct.name;
            ThemeName = settings.Theme.Name;
            if (settings.CurrentUserProfile != null)
            {
                ProfileName = settings.CurrentUserProfile.Name;
            }
            Saver.SaveToJson(this, "User");
        }

    }
}
