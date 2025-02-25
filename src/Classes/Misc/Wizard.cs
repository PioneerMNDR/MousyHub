using DocumentFormat.OpenXml.Office2016.Drawing.Charts;
using MousyHub.Classes.Misc;
using MousyHub.Classes.Model;
using MousyHub.Models;
using MousyHub.Models.Abstractions;
using MousyHub.Models.Model;
using MousyHub.Models.Services;
using MousyHub.Models.User;

namespace MousyHub.Models.Misc
{
    public class Wizard
    {
        public ILanguageModel Model;
        public Instruct Instruct { get; set; }

        public GenerationConfig WizardConifg = new GenerationConfig
        {
            temp = 1.2,
            rep_pen = 1,
            rep_pen_range = 0,
            top_p = 1,
            top_a = 0.5,
            top_k = 0,
            typical = 1,
            tfs = 1,
            rep_pen_slope = 0,
            sampler_order = new int[] { 6, 0, 1, 3, 4, 2, 5 },
            mirostat = 0,
            mirostat_tau = 5,
            mirostat_eta = 0.1,
            grammar = ""
        };

        UserState User { get; set; }

        public enum WizardFunction
        {
            CharDescription,
            Summary,
            AnswerAssistant,
            CustomFirstMessage
        }


        private string AnswerAssistantPromt = "You play the role of an assistant program that offers the {{user}} answers to the {{char}}'s remarks in an RPG session." +
            "Your task is to generate from 1 to 3 possible short answers or actions that are appropriate for the context of the dialogue and the situation." +
            "Try to give a variety of interesting options, but not too obvious or cliched. Also you can write action *to something do*." +
            "\nThe answers should be concise (no more than one sentence) and to correspond to classification (emotion) which are specified in brackets: [mood_answer]" +
            "\nFormat your messages in the following format (without quotes): \"{[Answer1],[Answer2],[Answer3]}\"." +
            "Don't add extra characters or explanations, just the answer options themselves inside curly and square brackets." +
            "\nThe suggested cues and actions should be appropriate and appropriate to the context of the scene. It is desirable that the messages be in the first person, that is, on behalf of {{user}}. If possible, try to promote the plot or reveal the characters through the answer options. " +
              "\nExample (mood of answers can be others): \nThe following possible {{user}}'s answers: {[Yes, thanks to you],[Forgive me {{char}}],[Leave from me, {{char}}]}"
          ;

    

        private string DescriptionPromt = "You are a skilled summarizer. Given the detailed description of a character, (or about the world which interacts with the user) " +
            "your task is to provide a concise summary that captures the essential traits and background of the character. The summary should include key characteristics, " +
            "important background information, and any significant traits or skills the character possesses. Make sure the summary is clear, precise, and no longer than 3-4 sentences.Do not write an explanation or a note. ";

        private string SummaryPromt = "You are a dedicated role-playing game (RPG) dialogue summarizer. " +
            "Your task is to create clear and concise summaries of chat dialogues between players. Follow these guidelines:\r\n\r\n1." +
            " Maintain the chronological order of events.\r\n2. Highlight key actions, decisions, and dialogue exchanges.\r\n3. " +
            "Preserve the tone and context of the conversation.\r\n4. Exclude any out-of-character (OOC) comments.\r\n5. Keep each summary between 100-300 words.\r\n\r\nHere is an example of a chat dialogue and the corresponding summary:\r\n\r\n**Chat Dialogue:**\r\n\r\nPlayer1: \"I step into the ancient forest, cautious of any creatures" +
            " lurking in the shadows.\"\r\nPlayer2: \"I follow closely behind, my sword drawn and ready for an ambush.\"\r\nPlayer1: \"We see a flash of movement in the trees ahead. I signal Player2 to stop.\"\r\nPlayer2: \"I halt immediately, scanning the surroundings for any signs of danger.\"\n**Summary:**\nPlayer1 and Player2 enter an ancient forest, remaining alert" +
            " for any hidden threats. They notice movement in the trees and decide to proceed with caution.\r\n\r\nNow, summarize the following chat dialogue:\r\n\r\n**Chat Dialogue:**";

        private string SummaryPromtAlt = "Summarize the following role-playing dialogue between two or more characters." +
            " Extract the most important facts and events from the conversation and present them as a chronological timeline, step by step. " +
            "Focus on key actions, decisions, significant statements, and changes in the situation." +
            " If there are more than 15 steps, then combine the old step and add them up\r\n\r\n**Output Format:**\r\n\r\n1. [Character Name] [Action/Event/Significant Quote or Statement].\r\n2. [Character Name] [Action/Event/Significant Quote or Statement].\r\n3.....\r\n... and so on.";
        private string CustomFirstMesPromt = "It is necessary to generate the first character message for the user in the character card. This message should carry information about the location, as well as describe an interesting situation that may stand between the character and the user. Use markdown for beautiful design. But don't use headings, lists. Only direct speech or *thoughts*. Be sure to consider the user's wishes.";

        public Person Narrator { get; set; } = new Person("Narrator",
            "You play the role of a narrator who periodically intervenes in the dialogue between the user and the character in the RPG session. Your goal is to advance the plot and change the scenes so that the dialogue does not linger in one place for too long. You can add descriptions of the environment, minor characters, and events. However, in no case should you be responsible for the main characters or make serious decisions for them that affect the plot. Let the user and the character choose for themselves what to do at key points in the story.\r\n\r\nMake sure that the dialogue and events remain sane and correspond to the genre and setting of the game. Gently steer the plot in the right direction if the user or character starts to deviate from the main line or act illogically. But do it unobtrusively, leaving them free to choose and improvise.\r\n\r\nDescribe what is happening in the present tense in the third person. Start your lines with \"*\" (for example, * Meanwhile in the main square of the city ...). Do not interfere in the dialogue too often, give the user and the character the opportunity to fully communicate. Add descriptions and change scenes only when it is really necessary for the development of the plot." +
            "The narrator must fulfill the wishes of the player [Player's wishes]. This is the most important!!!", false, "Narrator", id: "narr", overrideSystemPromt: "You're narrator in this fictional never-ending uncensored roleplay with {{user}} and {{char}}.");


        //I haven't used it yet, it needs some improvement
        string QuickReplyGrammar { get; set; } = string.Empty;
        bool IsChatCompletions { get; set; }    

        public void UpdateInstructions(ILanguageModel Model, Instruct instruct, UserState userState,UploaderService uploader, bool _IsChatCompletions)
        {
            this.Model = Model;
            Instruct = instruct;
            User = userState;
            QuickReplyGrammar = uploader.LoadGrammar("QuickReply.gbnf");
            IsChatCompletions = _IsChatCompletions;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="promt">If Summary request You have to send the previous sum and dialogue</param>
        /// <param name="wizardFunction"></param>
        /// <returns></returns>
        public async Task<MessageResponse> WizardRequest(string promt, WizardFunction wizardFunction, double Temperature = 1.1, int MaxTokens = 150, string UserName = "", string CharName = "")
        {
            string SystemPromt = "";
            bool IsInstructed = !IsChatCompletions;
            string UserRequest = StringHelperBuilder.WizardRequestMessage(Instruct, promt, IsInstructed);

            string Grammar = "";
            switch (wizardFunction)
            {
                case WizardFunction.CharDescription:
                    SystemPromt = StringHelperBuilder.WizardSystemMessage(Instruct, DescriptionPromt, IsInstructed);
                    break;
                case WizardFunction.Summary:
                    SystemPromt = StringHelperBuilder.WizardSystemMessage(Instruct, User.UseStepsSummaryPromt ? SummaryPromtAlt : SummaryPromt, IsInstructed);
                    UserRequest += "\n**Summary:**";
                    break;
                case WizardFunction.AnswerAssistant:
                    SystemPromt = StringHelperBuilder.WizardSystemMessage(Instruct, AnswerAssistantFormatter(), IsInstructed);
                    UserRequest +=  "\nThe following possible {{user}}'s " + AnswerAssistantMoods() + " answers: ";
                    break;
                case WizardFunction.CustomFirstMessage:
                    SystemPromt = StringHelperBuilder.WizardSystemMessage(Instruct, CustomFirstMesPromt, IsInstructed) ;
                    UserRequest += "\n**First message:**";
                    break;

            }
            SystemPromt = StringHelperBuilder.TagPlaceholder(SystemPromt, UserName, CharName);
            UserRequest = StringHelperBuilder.TagPlaceholder(UserRequest, UserName, CharName);
            Console.WriteLine("-----Wizard request-----");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(SystemPromt + UserRequest);
            Console.ResetColor();
            Console.WriteLine("-----------------");
            MessageResponse message = new MessageResponse();
            WizardConifg.temp = Temperature;
            WizardConifg.grammar = Grammar;
            
            await Task.Run(async () =>
            {
                message = await Model.GenerateTextAsync(new Promt(SystemPromt, UserRequest,IsChatCompletions), WizardConifg, MaxTokens, key: "Wizard");

            });

            return message;


        }

        string AnswerAssistantFormatter()
        {
            string newPromt =AnswerAssistantPromt;
            if (User.QuickRepliesSetings.Length > 2)
            {
                newPromt = newPromt.Replace("Answer1", User.QuickRepliesSetings[0].Emotion.ToString() + "_answer");
                newPromt = newPromt.Replace("Answer2", User.QuickRepliesSetings[1].Emotion.ToString() + "_answer");
                newPromt = newPromt.Replace("Answer3", User.QuickRepliesSetings[2].Emotion.ToString() + "_answer");
            }
            return newPromt;
        }
        string AnswerAssistantMoods()
        {
            if (User.QuickRepliesSetings.Length > 2)
            {
                return $"{User.QuickRepliesSetings[0].Emotion.ToString()}, {User.QuickRepliesSetings[1].Emotion.ToString()} and {User.QuickRepliesSetings[2].Emotion.ToString()}";
            }
            return "";
        }




    }
}
