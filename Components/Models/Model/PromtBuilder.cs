namespace MousyHub.Components.Models.Model
{
    public static class PromtBuilder
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



        public static string SystemMessageShort(ChatHistory chatHistory)
        {
            string story_string = "";
            story_string = "\n{{Char}}\'s short description: " + chatHistory.CharShortDesription + separator + "\n{{User}}\'s description: " + chatHistory.MainUser.Description + separator + "Scenario: " + chatHistory.Scenario + separator;
            story_string = TagPlaceholder(story_string, chatHistory.MainUser.Name, chatHistory.MainCharacter.Name);
            return story_string;
        }
        public static string UserMessage(Instruct instruct, string promt, string personName)
        {
            var n1 = personName + ": ";
            //var n2 = botname + ": ";
            if (instruct.names == false)
            {
                n1 = "";/* n2 = ""*/;
            }
            if (instruct.macro)
            {
                instruct.input_sequence.Replace("{{name}}", personName);
                //instruct.output_sequence.Replace("{{name}}", botname);
            }
            if (instruct.wrap && instruct.input_suffix == "")
            {
                instruct.input_suffix = "\n";
            }
            if (instruct.wrap && instruct.output_suffix == "")
            {
                instruct.output_suffix = "\n";
            }
            //string e = instruct.input_suffix + instruct.input_sequence + separator + n1 + promt + instruct.output_suffix + instruct.output_sequence + separator + n2;

            string e = instruct.input_suffix + instruct.input_sequence + separator + n1 + promt + instruct.output_suffix + instruct.output_sequence;
            return e;
        }

        public static string BotMessageFormatting(Instruct instruct, string personName)
        {
            var n2 = separator + personName + ": ";
            if (instruct.names == false)
            {
                n2 = "";
            }
            if (instruct.macro)
            {
                instruct.output_sequence.Replace("{{name}}", personName);
            }
            string content = n2;
            return content;
        }

        public static string TagPlaceholder(string message, string Username, string CharacterName)
        {
            string result = message.Replace("{{User}}", Username).Replace("{{user}}", Username).Replace("{{char}}", CharacterName).Replace("{{Char}}", CharacterName);

            return result;
        }
        public static string[] Stop_sequence_split(string stop_s)
        {
            var seq = stop_s.Split(',', options: StringSplitOptions.None);
            return seq;
        }



        public static string WizardSystemMessage(Instruct instruct, string systemPromt)
        {
            string system = instruct.system_sequence + separator + systemPromt + separator;
            return system;
        }


        public static string WizardRequestMessage(Instruct instruct, string request)
        {
            string e = instruct.input_suffix + instruct.input_sequence + separator + request + instruct.output_suffix + instruct.output_sequence + separator;
            return e;
        }
        public static string ToLiteral(string input)
        {

            return Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(input, false);
        }

    }
}
