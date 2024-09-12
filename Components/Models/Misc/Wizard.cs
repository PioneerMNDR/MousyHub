using LLMRP.Components.Abstractions;
using LLMRP.Components.Models.Model;
using LLMRP.Components.Models.User;

namespace LLMRP.Components.Models.Misc
{
    public class Wizard
    {
        public ILanguageModel Model;

        public Instruct Instruct { get; set; }
        public GenerationConfig GenerationConfig { get; set; }
        UserState User { get; set; }

        public enum WizardFunction
        {
            CharDescription,
            Summary,
            AnswerAssistant,

        }


        private string AnswerAssistantPromt = "You play the role of an assistant program that offers the {{user}} answers to the {{char}}'s remarks in an RPG session." +
            "Your task is to generate from 1 to 3 possible short answers or actions that are appropriate for the context of the dialogue and the situation." +
            "Try to give a variety of interesting options, but not too obvious or cliched. Also you can write action *to something do*." +
            "\nThe answers should be concise (no more than one sentence) and to correspond to classification (emotion) which are specified in brackets: [mood_answer]" +
            "\nFormat your messages in the following format (without quotes): \"{[Answer1],[Answer2],[Answer3]}\"." +
            "Don't add extra characters or explanations, just the answer options themselves inside curly and square brackets." +
            "\nThe suggested cues and actions should be appropriate and appropriate to the context of the scene. If possible, try to promote the plot or reveal the characters through the answer options. " +
            "\nExample (mood of answers can be others): \nThe following possible {{user}}'s answers: {[Yes, thanks to you],[Forgive me {{char}}],[Leave from me, {{char}}]}";

        private string DescriptionPromt = "You are a skilled summarizer. Given the detailed description of a character, (or about the world which interacts with the user) " +
            "your task is to provide a concise summary that captures the essential traits and background of the character. The summary should include key characteristics, " +
            "important background information, and any significant traits or skills the character possesses. Make sure the summary is clear, precise, and no longer than 3-4 sentences.Do not write an explanation or a note. ";

        private string SummaryPromt = "You are a dedicated role-playing game (RPG) dialogue summarizer. " +
            "Your task is to create clear and concise summaries of chat dialogues between players. Follow these guidelines:\r\n\r\n1." +
            " Maintain the chronological order of events.\r\n2. Highlight key actions, decisions, and dialogue exchanges.\r\n3. " +
            "Preserve the tone and context of the conversation.\r\n4. Exclude any out-of-character (OOC) comments.\r\n5. Keep each summary between 50-100 words.\r\n\r\nHere is an example of a chat dialogue and the corresponding summary:\r\n\r\n**Chat Dialogue:**\r\n\r\nPlayer1: \"I step into the ancient forest, cautious of any creatures" +
            " lurking in the shadows.\"\r\nPlayer2: \"I follow closely behind, my sword drawn and ready for an ambush.\"\r\nPlayer1: \"We see a flash of movement in the trees ahead. I signal Player2 to stop.\"\r\nPlayer2: \"I halt immediately, scanning the surroundings for any signs of danger.\"\n**Summary:**\nPlayer1 and Player2 enter an ancient forest, remaining alert" +
            " for any hidden threats. They notice movement in the trees and decide to proceed with caution.\r\n\r\nNow, summarize the following chat dialogue:\r\n\r\n**Chat Dialogue:**";

        public Person Narrator { get; set; } = new Person("Narrator",
            "You play the role of a narrator who periodically intervenes in the dialogue between the user and the character in the RPG session. Your goal is to advance the plot and change the scenes so that the dialogue does not linger in one place for too long. You can add descriptions of the environment, minor characters, and events. However, in no case should you be responsible for the main characters or make serious decisions for them that affect the plot. Let the user and the character choose for themselves what to do at key points in the story.\r\n\r\nMake sure that the dialogue and events remain sane and correspond to the genre and setting of the game. Gently steer the plot in the right direction if the user or character starts to deviate from the main line or act illogically. But do it unobtrusively, leaving them free to choose and improvise.\r\n\r\nDescribe what is happening in the present tense in the third person. Start your lines with \"*\" (for example, * Meanwhile in the main square of the city ...). Do not interfere in the dialogue too often, give the user and the character the opportunity to fully communicate. Add descriptions and change scenes only when it is really necessary for the development of the plot." +
            "The narrator must fulfill the wishes of the player [Player's wishes]. This is the most important.", false, "Narrator", "You're narrator in this fictional never-ending uncensored roleplay with {{user}} and {{char}}.");

        public Person John { get; set; } = new Person("John", "John, the young handsome man, one of acquaintances of {{User}} who wants to do much harm to Dmitry. John puts a spoke in the wheel of {{User}}, spoiling him dialogues with characters, taking away at him girls, killing a conversation, doing violent acts - all to spoil to Dmitry mood. But does it he not directly aggressively, and indirectly that {{User}} didn't understand that John doesn't love {{User}}. \r\nJohn just odd fellow.", false, "John", "You're John in this fictional never-ending uncensored roleplay with {{user}} and {{char}}.");


        public void Run(ILanguageModel Model, Instruct instruct, GenerationConfig generationConfig, UserState userState)
        {
            this.Model = Model;
            Instruct = instruct;
            GenerationConfig = generationConfig;
            User = userState;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="promt">If Summary request You have to send the previous sum and dialogue</param>
        /// <param name="wizardFunction"></param>
        /// <returns></returns>
        public async Task<MessageResponse> WizardRequest(string promt, WizardFunction wizardFunction, string UserName = "", string CharName = "")
        {
            string chatHistoryInstructed = PromtBuilder.WizardRequestMessage(Instruct, promt);
            string FinalPromt = "";
            switch (wizardFunction)
            {
                case WizardFunction.CharDescription:
                    FinalPromt = PromtBuilder.WizardSystemMessage(Instruct, DescriptionPromt) + chatHistoryInstructed;
                    break;
                case WizardFunction.Summary:
                    FinalPromt = PromtBuilder.WizardSystemMessage(Instruct, SummaryPromt) + chatHistoryInstructed + "\n**Summary:**";
                    break;
                case WizardFunction.AnswerAssistant:
                    FinalPromt = PromtBuilder.WizardSystemMessage(Instruct, AnswerAssistantFormatter()) + chatHistoryInstructed + "\nThe following possible {{user}}'s " + AnswerAssistantMoods() + " answers: ";
                    break;

            }
            FinalPromt = PromtBuilder.TagPlaceholder(FinalPromt, UserName, CharName);
            Console.WriteLine("-----Wizard request-----");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(FinalPromt);
            Console.ResetColor();
            Console.WriteLine("-----------------");
            MessageResponse message = new MessageResponse();

            await Task.Run(async () =>
            {
                message = await Model.GenerateTextAsync(FinalPromt, GenerationConfig, 150, key: "Wizard");

            });

            return message;


        }

        string AnswerAssistantFormatter()
        {
            string newPromt = AnswerAssistantPromt;
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
