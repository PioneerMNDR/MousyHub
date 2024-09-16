using GTranslate.Translators;
using System.Text.RegularExpressions;

namespace LLMRP.Components.Models.Services
{
    public class TranslatorService
    {
        private readonly SettingsService Settings;
        private ITranslator _translator;
        public bool isEnabled { get { return Settings.User.TranslatorOptions.isEnabled; }  set { Settings.User.TranslatorOptions.isEnabled = value; } }

        private int RequestCount = 0;

        public TranslatorService(SettingsService settings)
        {
            Settings = settings;
            ChangeProvider(Settings.User.TranslatorOptions.CurrentService);
        }

        public void ChangeProvider(GTranslate.TranslationServices service)
        {
            switch (service)
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
            try
            {
                var result = await _translator.TranslateAsync(text, "en", Settings.User.TranslatorOptions.SelectLanguage.Value);
                RequestCount++;
                Console.WriteLine("TranslatorRequestCount:" + RequestCount);
                return FixFormatting(result.Translation);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return text;                
            }
       
        }
        public async Task<string> TranslateForUser(string text)
        {
            try
            {
                var result = await _translator.TranslateAsync(text, Settings.User.TranslatorOptions.SelectLanguage.Value, "en");
                RequestCount++;
                Console.WriteLine("TranslatorRequestCount:" + RequestCount);
                //Console.WriteLine("OriginalText:" + text);
                //Console.WriteLine("TranslatedText:" + result.Translation);
                return FixFormatting(result.Translation);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return text;
            }
  

        }
        public string FixFormatting(string input)
        {
            if (Settings.User.TranslatorOptions.CorrectedFormatting)
            {
                if (string.IsNullOrEmpty(input))
                {
                    return input;
                }
                return Regex.Replace(input, @"\*\s", "*");
            }
            return input;
    
        }

    }
}
