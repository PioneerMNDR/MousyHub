using GTranslate.Translators;

namespace LLMRP.Components.Models.Services
{
    public class TranslatorService
    {
        private readonly SettingsService Settings;
        private ITranslator _translator;
        public bool isEnabled { get { return Settings.User.TranslatorOptions.isEnabled; }  set { Settings.User.TranslatorOptions.isEnabled = value; } }

        public TranslatorService(SettingsService settings)
        {
            Settings = settings;
            switch (settings.User.TranslatorOptions.CurrentService)
            {
                case GTranslate.TranslationServices.Google:
                    _translator = new GoogleTranslator();
                    break;
                case GTranslate.TranslationServices.Yandex:
                    _translator = new YandexTranslator();
                    break;
                case GTranslate.TranslationServices.Bing:
                    _translator = new BingTranslator();
                    break;
                case GTranslate.TranslationServices.Microsoft:
                    _translator = new MicrosoftTranslator();
                    break;
                default:
                    _translator = new GoogleTranslator();
                    break;
            }
        }
        public async Task<string> TranslateForLLM(string text)
        {
            var result = await _translator.TranslateAsync(text, "en", Settings.User.TranslatorOptions.SelectLanguage.Value);
            return result.Translation;
        }
        public async Task<string> TranslateForUser(string text)
        {
            var result = await _translator.TranslateAsync(text, Settings.User.TranslatorOptions.SelectLanguage.Value, "en");
            return result.Translation;
        }

    }
}
