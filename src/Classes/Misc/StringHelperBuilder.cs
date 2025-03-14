using Microsoft.IdentityModel.Tokens;
using MousyHub.Models;
using MousyHub.Models.Model;
using System.Text.RegularExpressions;

namespace MousyHub.Classes.Misc
{
    static class StringExtensions
    {
        public static string[] SplitWithDefaultSeparators(this string text)
        {
            string[] separators = GenerateSeparatorVariations();
            return text.SplitKeepSeparators(separators);
        }
        public static string[] GenerateSeparatorVariations(params string[] baseSeparators)
        {
            // Базовые символы, которые будут использоваться как основа
            char[] baseChars = { '.', '!', '?', ':' };

            // Дополнительные элементы, которые могут следовать за базовым символом
            string[] additions = { " ", "\" ", "*", "'" };

            string[] specChars = { ",* " };

            // Специальные комбинации для многоточия
            string[] ellipsisAdditions = { " ", "*", "\"" };

            List<string> allVariations = new List<string>();

            // Обрабатываем базовые символы
            foreach (char baseChar in baseChars)
            {
                // Добавляем стандартные комбинации для каждого базового символа
                foreach (string addition in additions)
                {
                    allVariations.Add(baseChar + addition);
                }

                // Добавляем специальную обработку для многоточия, если базовый символ - точка
                if (baseChar == '.')
                {
                    foreach (string addition in ellipsisAdditions)
                    {
                        allVariations.Add("..." + addition);
                    }
                }
            }
            foreach (var item in specChars)
            {
                allVariations.Add(item);
            }

            return allVariations.ToArray();
        }
        public static string[] SplitKeepSeparators(this string s, params string[] separators)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (s.Length == 0) return new string[0];

            var result = new List<string>();
            int startIndex = 0;

            while (startIndex < s.Length)
            {
                int earliestIndex = -1;
                string earliestSeparator = null;

                foreach (var separator in separators)
                {
                    int index = s.IndexOf(separator, startIndex);
                    if (index != -1 && (earliestIndex == -1 || index < earliestIndex))
                    {
                        earliestIndex = index;
                        earliestSeparator = separator;
                    }
                }

                if (earliestIndex == -1)
                {
                    // Нет больше разделителей - текст не заканчивается разделителем,
                    // поэтому мы не добавляем оставшуюся часть
                    break;
                }

                // Добавляем предложение вместе с разделителем
                string currentSentence = s.Substring(startIndex, earliestIndex - startIndex + earliestSeparator.Length).Trim();
                result.Add(currentSentence);

                startIndex = earliestIndex + earliestSeparator.Length;
            }
    
            return result.ToArray();
        }

    }
    public static class StringHelperBuilder
    {


        static string separator = "\n";

        
        public static string SystemMessage(Instruct instruct, ChatHistory chatHistory, Person person)
        {
            string story_string = "";
            string system = "";

            if (person.OverrideSystemPromt != null)
                system = instruct.system_sequence + separator + person.OverrideSystemPromt;
            else
                system = instruct.system_sequence + separator + instruct.system_prompt;

            if (person == chatHistory.MainCharacter)
                story_string = system + separator + "{{Char}}\' s description:" + chatHistory.CharDesription + separator + "{{Char}}\' s message examples:" + chatHistory.Mes_Example + separator + "Scenario: " + chatHistory.Scenario;
            else
                story_string = system + separator + $"{person.Name}'s description:" + person.Description + separator + "{{Char}}\'s short description:" + chatHistory.CharShortDesription + separator + "Scenario: " + chatHistory.Scenario;

            if (chatHistory.SummarizeContext != null && chatHistory.SummarizeContext != "")
            {
                story_string += separator + "The summary the events in dialogue: " + chatHistory.SummarizeContext;
            }

            story_string = TagPlaceholder(story_string, chatHistory.MainUser.Name, chatHistory.MainCharacter.Name);
            return story_string;
        }
        public static string TagPlaceholder(string message, string Username, string CharacterName)
        {
            if (!string.IsNullOrEmpty(message))
            {
                string result = message.Replace("{{User}}", Username).Replace("{{user}}", Username).Replace("{{char}}", CharacterName).Replace("{{Char}}", CharacterName);
                return result;
            }
        
            return string.Empty;
         
        }
        public static string SystemMessageShort(ChatHistory chatHistory)
        {
            string story_string = "";
            story_string = "\n{{Char}}\'s short description: " + chatHistory.CharShortDesription + separator + "\n{{User}}\'s description: " + chatHistory.MainUser.Description + separator + "Scenario: " + chatHistory.Scenario + separator;
            story_string = TagPlaceholder(story_string, chatHistory.MainUser.Name, chatHistory.MainCharacter.Name);
            return story_string;
        }

        /// <returns>Returns a phrase for a chat like: '<|im_end|><|im_start|>user\UserName: '</returns>
        public static string UserMessageInstructed(Instruct instruct, string promt, string personName)
        {
            var n1 = personName + ": ";
            if (instruct.names == false)
            {
                n1 = "";
            }
            if (instruct.macro)
            {
                instruct.input_sequence.Replace("{{name}}", personName);
            }
            if (instruct.wrap && string.IsNullOrEmpty(instruct.input_suffix))
            {
                instruct.input_suffix = "\n";
            }
            string e = instruct.input_suffix + instruct.input_sequence + n1 + promt;
            return e;
        }


        /// <returns>Returns a phrase for a chat like: '<|im_end|><|im_start|>assistant\nCharName: '</returns>
        public static string BotMessageInstructed(Instruct instruct, string personName)
        {
            var n2 = personName + ": ";
            if (instruct.names == false)
            {
                n2 = "";
            }
            if (instruct.macro)
            {
                instruct.output_sequence.Replace("{{name}}", personName);
            }
  
            if (instruct.wrap && string.IsNullOrEmpty(instruct.output_suffix))
            {
                instruct.output_suffix = "\n";
            }
            string content = instruct.output_suffix + instruct.output_sequence + n2;
            return content;
        }


        public static string[] Stop_sequence_split(string stop_s)
        {
            if (string.IsNullOrEmpty(stop_s))
            {
                return Array.Empty<string>();
            }
            var seq = stop_s.Split(',', options: StringSplitOptions.None);
            return seq;
        }



        public static string WizardSystemMessage(Instruct instruct, string systemPromt, bool isInstructed)
        {
            if (isInstructed) 
               return instruct.system_sequence + separator + systemPromt + separator;
            else
                return systemPromt + separator;
        }


        public static string WizardRequestMessage(Instruct instruct, string request, bool isInstructed)
        {
            if (isInstructed)
                return instruct.input_suffix + instruct.input_sequence + separator + request + instruct.output_suffix + instruct.output_sequence + separator;
            else
                return request;
        }
        public static string RemoveAsterisks(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return Regex.Replace(text, @"\*+", "");
        }
        public static string ToLiteral(string input)
        {

            return Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(input, false);
        }

    }
}
