﻿using MousyHub.Components.Models.LLama;
using MousyHub.Components.Models.Misc;
using MousyHub.Components.Models.Services;

namespace MousyHub.Components.Models.User
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
        public VoskOptions VoskOptions { get; set; } = new VoskOptions();
        public int CurrentMaxToken { get; set; } = 100;
        public int CurrentContextLength { get; set; } = 4096;
        public bool PauseBeforeGenerating { get; set; } = false;
        public bool AutoSummarize { get; set; } = true;

        public int SummarizeMessageCount = 10;
        public bool AnswerAssistant { get; set; } = false;
        public bool SquareAvatars { get; set; } = false;
        public QuickReplySetting[] QuickRepliesSetings { get; set; } = { new QuickReplySetting(QuickReplySetting.ResponseEmotion.Positive), new QuickReplySetting(QuickReplySetting.ResponseEmotion.Neutral), new QuickReplySetting(QuickReplySetting.ResponseEmotion.Negative) };

        public void SaveSettings(SettingsService settings)
        {
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
