using DocumentFormat.OpenXml.Bibliography;
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
                if (string.IsNullOrEmpty(text))
                {
                    return text;
                }
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
                if (string.IsNullOrEmpty(text))
                {
                    return text;
                }
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
                bool isNewLine = true; // Флаг для отслеживания начала новой строки

                for (int i = 0; i < input.Length; i++)
                {
                    char currentChar = input[i];

                    // Проверка на начало новой строки с маркером списка (*, -, +)
                    if (isNewLine && (currentChar == '*' || currentChar == '-' || currentChar == '+') &&
                        i + 1 < input.Length && input[i + 1] == ' ')
                    {
                        // Для тире можно заменить на длинное тире
                        if (currentChar == '-')
                            result.Append("—");
                        else
                            result.Append(currentChar); // Для * и + просто добавляем символ

                        i++; // Пропускаем пробел после маркера
                        isNewLine = false;
                        continue;
                    }

                    // Проверка на случай "* - " (звездочка, пробел, тире, пробел)
                    if (currentChar == '*' && i + 3 < input.Length &&
                        input[i + 1] == ' ' && input[i + 2] == '-' && input[i + 3] == ' ')
                    {
                        result.Append('*'); // Добавляем звездочку
                        result.Append("—"); // Заменяем " - " на длинное тире
                        i += 3; // Пропускаем " - "
                        continue;
                    }

                    if (currentChar == '\n' || currentChar == '\r')
                    {
                        isNewLine = true;
                        result.Append(currentChar);
                    }
                    else if (currentChar == '*')
                    {
                        starCount++;
                        result.Append(currentChar);
                        isNewLine = false;
                    }
                    else if (currentChar == ' ' && starCount % 2 == 1 && i > 0 && input[i - 1] == '*')
                    {
                        // Пропускаем пробел после нечетного количества звездочек
                        isNewLine = false;
                    }
                    else
                    {
                        result.Append(currentChar);
                        isNewLine = false;
                    }
                }

                return result.ToString();
            }
            return input;
        }


    }
}
