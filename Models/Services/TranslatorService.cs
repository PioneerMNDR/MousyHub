using GTranslate.Translators;
using System.Text;
using System.Text.RegularExpressions;

namespace MousyHub.Models.Services
{
    public class TranslatorService
    {
        private readonly SettingsService Settings;
        private ITranslator _translator;
        public bool isEnabled { get { return Settings.User.TranslatorOptions.isEnabled; } set { Settings.User.TranslatorOptions.isEnabled = value; } }

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

                int starCount = 0;
                StringBuilder result = new StringBuilder(input.Length);

                for (int i = 0; i < input.Length; i++)
                {
                    char currentChar = input[i];

                    if (currentChar == '*')
                    {
                        starCount++;
                        result.Append(currentChar);
                    }
                    else if (currentChar == ' ' && starCount % 2 == 1 && i > 0 && input[i - 1] == '*')
                    {
                        // Skip this space because it is after an odd count of stars and directly follows a star.
                    }
                    else
                    {
                        result.Append(currentChar);
                    }
                }

                return result.ToString();
            }
            return input;
        }


    }
}
