using DocumentFormat.OpenXml.Bibliography;
using MousyHub.Models.Misc;
using MousyHub.Models.Model;
using MousyHub.Models.Services;
using Newtonsoft.Json;

namespace MousyHub.Models
{
    public class ChatHistory
    {

        public ChatHistory() { }
        public ChatHistory(Person UserPerson, Person CharacterPerson, List<Person> AllPerson)
        {
            Messages = new List<Message>();
            ChatLoading(UserPerson, CharacterPerson, AllPerson);



        }
        public void ChatLoading(Person UserPerson, Person CharacterPerson, List<Person> AllPerson)
        {

            foreach (Person person in AllPerson)
            {
                foreach (var message in Messages)
                {
                    if (message.IdOwner == person.Id)
                    {
                        message.Owner = person;
                    }
                }
                foreach (var alt in AlterativeFirstMessages)
                {
                    if (alt.IdOwner == person.Id)
                    {
                        alt.Owner = person;
                    }
                }
                foreach (var speaker in SpeakerQueue)
                {
                    if (speaker.Id == person.Id)
                    {
                        speaker.Person = person;
                    }
                }
            }

            //If person is null. Delete this
            SpeakerQueue.Where(x => x.Person == null).ToList().ForEach(x => DeleteInQueue(x.Person));

            DefaultQueue();
            MainCharacter = CharacterPerson;
            MainUser = UserPerson;
            ChatName = CharacterPerson.CharacterCard.system_name;
            if (CharacterPerson.CharacterCard != null)
            {
                Mes_Example = CharacterPerson.CharacterCard.data.mes_example;
                FirstMessage = CharacterPerson.CharacterCard.data.first_mes;
                Scenario = CharacterPerson.CharacterCard.data.scenario;
                CharDesription = CharacterPerson.CharacterCard.data.description;
                Personality = CharacterPerson.CharacterCard.data.personality;
                CharShortDesription = CharacterPerson.CharacterCard.data.short_description;
                Alt_greetings = CharacterPerson.CharacterCard.data.alternate_greetings;
            }
            AddToQueue(MainUser, true);
            AddToQueue(MainCharacter, true);
        }


        public string ChatName { get; set; }
        public string? Mes_Example { get; set; }
        public string? Scenario { get; set; }
        //Buffer for the initial Scenario if the original is rewritten
        public string? InitialScenario { get; set; }
        public string? FirstMessage { get; set; }
        public string? CharDesription { get; set; }
        public string? CharShortDesription { get; set; }

        public string SystemMessage { get; set; }

        public string? Personality { get; set; }

        public string?[] Alt_greetings { get; set; }

        public string? SummarizeContext { get; set; }
        public string? PlayerWishes { get; set; }
        public string? OCC_PlayerWishes { get; set; }
        /// <summary>
        /// Stores the value received from RAG, reset when used
        /// </summary>
        public string? MemoryFromChat { get; set; }

        public int? ChatContextSize { get; set; }


        public List<Message> Messages { get; set; }

        public List<Message> AlterativeFirstMessages { get; set; } = new List<Message>();

        public List<Speaker> SpeakerQueue { get; set; } = new List<Speaker>();
        [JsonIgnore]
        public Person MainUser { get; set; }
        [JsonIgnore]
        public Person MainCharacter { get; set; }


        /// <summary>
        /// Prepares for loading in LLM the system message and also all history of dialogue
        /// </summary>
        /// <param name="instruct"></param>
        /// <returns></returns>
        public async Task<string> PromtToLLM(Instruct instruct, Person person, bool console = true)
        {
            SystemMessage = PromtBuilder.SystemMessage(instruct, this, person);
            string finalpromt = SystemMessage;
            //Special condition for the narrator
            if (person.Name == "Narrator" && !string.IsNullOrEmpty(PlayerWishes))
            {
                finalpromt += "\n[Player's wishes: " + PlayerWishes + "]";
            }
            //Memory From RAG
            if (!string.IsNullOrEmpty(MemoryFromChat))
            {
                finalpromt += "\n[Early Memories from Chat (Possibly for use): {" + MemoryFromChat + "}]";
                MemoryFromChat = string.Empty;
            }
            Message lastmessage = new Message();
            foreach (var item in Messages)
            {
                if (item.Owner.IsUser && item.isSummarized == false)
                {

                    finalpromt += item.InstructContent;
                }
                else if (item.isSummarized == false)
                {
                    //If the bot answers behind other bot, then we need to follow a template
                    if (lastmessage.Owner != null && lastmessage.Owner.IsUser == false)
                    {
                        finalpromt += instruct.output_sequence;
                    }
                    finalpromt += item.InstructContent;
                    finalpromt += item.Content;
                }
                lastmessage = item;
            }
            if (console)
            {
                Console.WriteLine("-----Promt-----");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(finalpromt);
                Console.ResetColor();
                Console.WriteLine("---------------");
            }

            return finalpromt;
        }
        public async Task<Message> AddMessage(string content, Person person, Instruct instruct, string NativeLangContent = "")
        {
            if (person.IsUser == true)
            {
                if (OCC_PlayerWishes != string.Empty) // <----Special condition for the OCC (add OOC to end of player message)
                {
                    content += "\n" + OCC_PlayerWishes;
                    OCC_PlayerWishes = string.Empty;
                }
                var InstructContent = PromtBuilder.UserMessage(instruct, content, person.Name);
                var newMes = new Message(content, InstructContent, person);
                newMes.UserNativeLanguageContent = NativeLangContent;
                Messages.Add(newMes);
                return newMes;
            }
            else
            {
                var InstructContent = PromtBuilder.BotMessageFormatting(instruct, person.Name);
                content = PromtBuilder.TagPlaceholder(content, MainUser.Name, MainCharacter.Name);
                var newMes = new Message(content, InstructContent, person);
                newMes.UserNativeLanguageContent = NativeLangContent;
                Messages.Add(newMes);
                return newMes;
            }

        }
        public async Task DeleteMessage(Message message)
        {
            Messages.Remove(message);
        }
        public async Task ClearIsGenerationBorder()
        {
            foreach (Message item in Messages)
            {
                item.isGenerating = false;
            }

        }
        public async Task DeleteLastMessage()
        {
            if (Messages.Count > 0)
            {
                Messages.Remove(Messages.Last());
            }

        }
        public void AddToQueue(Person person, bool isActive, bool isWriting = false)
        {
            if (SpeakerQueue.Any(x => x.Person == person) == false)
            {
                var s = new Speaker(SpeakerQueue.Count, person, isActive, isWriting);
                SpeakerQueue.Add(s);
                if (s.Order == 0 && s.isAction)
                {
                    s.isWriting = true;
                }
            }
        }
        /// <summary>
        /// Advances turn on +1 and returns the person which will speak to following. Ignores those who has skips
        /// </summary>
        /// <returns></returns>
        public Person QueueMoveOrder()
        {

            var firstItemIndex = SpeakerQueue.FindIndex(item => item.isAction == true);
            if (firstItemIndex == -1)
            {
                return MainUser;
            }
            var firstitem = SpeakerQueue[firstItemIndex];
            firstitem.isWriting = false;
            SpeakerQueue.Remove(firstitem);

            firstitem.Order = SpeakerQueue.Count(item => item.isAction == true);
            SpeakerQueue.Add(firstitem);
            int order = 0;

            var list = SpeakerQueue.Where(x => x.isAction == true).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].SkipNow == 0)
                {
                    list[i].Order = order++;

                    if (list[i].SkipCount != 0 && list[i].SkipNow == 0)
                    {
                        list[i].SkipNow = list[i].SkipCount;
                    }
                }
                else
                {
                    list[i].SkipNow--;
                }


            }
            SpeakerQueue = SpeakerQueue.OrderBy(x => x.Order).ToList();
            var nextItemIndex = SpeakerQueue.FindIndex(item => item.isAction == true);
            var nextItem = SpeakerQueue[nextItemIndex];
            nextItem.isWriting = true;
            return nextItem.Person;

        }
        public Person GetCurrentSpeakerInQueue()
        {

            var firstItemIndex = SpeakerQueue.FindIndex(item => item.isAction == true);
            if (firstItemIndex == -1)
            {
                return MainUser;
            }
            var firstitem = SpeakerQueue[firstItemIndex];
            return firstitem.Person;

        }
        private void DefaultQueue()
        {
            var user = SpeakerQueue.FirstOrDefault(x => x.Person.IsUser);
            if (user != null)
            {

                user.Order = 0;
                var sorted = SpeakerQueue.Where(x => x != user).OrderBy(p => Math.Abs(p.Order - user.Order)).ThenBy(p => p.Order).ToList();
                for (int i = 0; i < sorted.Count; i++)
                {
                    sorted[i].Order = i + 1;
                }
                foreach (var item in SpeakerQueue)
                {
                    if (item.Order == 0)
                    {
                        item.isWriting = true;
                    }
                    else
                    {
                        item.isWriting = false;
                    }
                }
                SpeakerQueue = SpeakerQueue.OrderBy(x => x.Order).ToList();

            }
        }

        public void DeleteInQueue(Person person)
        {
            var item = SpeakerQueue.Where(x => x.Person == person).FirstOrDefault();
            if (item != null)
            {
                if (item.Order == 0 && item.isAction)
                {
                    QueueMoveOrder();
                }
                SpeakerQueue.Remove(item);
            }
        }
        public Guid AddEmptyMessage(Person person, Instruct instruct)
        {
            //Add next person name to new emptyMessage content
            var content = PromtBuilder.BotMessageFormatting(instruct, person.Name);
            Message message = new Message("", content, person);
            message.isGenerating = true;
            Messages.Add(message);
            return message.GuidMessage;
        }
        public void FillAltFirstMessagesList(Person person, Instruct instruct)
        {
            AlterativeFirstMessages.Clear();
            var InstructContent = PromtBuilder.BotMessageFormatting(instruct, person.Name);
            AlterativeFirstMessages.Add(Messages[0]);
            foreach (var item in Alt_greetings)
            {
                string content = item;
                if (content == null) continue;
                content = PromtBuilder.TagPlaceholder(content, MainUser.Name, MainCharacter.Name);
                var newMes = new Message(content, InstructContent, person);
                AlterativeFirstMessages.Add(newMes);
            }
       

        }
        public void AddNewAltFirstMessage(Instruct instruct, string RawContent)
        {
            var InstructContent = PromtBuilder.BotMessageFormatting(instruct, MainCharacter.Name);
            var ProcContent = PromtBuilder.TagPlaceholder(RawContent, MainUser.Name, MainCharacter.Name);
            var newMes = new Message(ProcContent, InstructContent, MainCharacter);
            AlterativeFirstMessages.Add(newMes);
        }

        public async Task NextFirstMessage(bool NextIsLastAddedMessage=false,TranslatorService? translator = null)
        {
            if (AlterativeFirstMessages.Count > 1)
            {
                if (NextIsLastAddedMessage)
                {
                    // Получаем индекс последнего элемента в списке AlterativeFirstMessages
                    int lastIndex = AlterativeFirstMessages.Count - 1;
                    Messages[0] = AlterativeFirstMessages[lastIndex];
                }
                else
                {
                    int oldIndex = AlterativeFirstMessages.IndexOf(Messages[0]);
                    int newIndex = (oldIndex + 1) % AlterativeFirstMessages.Count;
                    Messages[0] = AlterativeFirstMessages[newIndex];
                }              
            }
            if (translator != null && string.IsNullOrEmpty(Messages[0].UserNativeLanguageContent))
            {
                await Messages[0].TranslateMessage(translator);
            }
        }

        public int IndexAltMessage()
        {
            if (AlterativeFirstMessages.Count>0)
            {
                if (AlterativeFirstMessages.IndexOf(Messages[0])==-1)
                {
                    return 0;
                }
                return AlterativeFirstMessages.IndexOf(Messages[0]);
            }
            return 0;
        }

        public Message GetCurrentFirstMessage()
        {
            if (Messages.Count>0)
            {
                return Messages[0];
            }
            return new Message();
        }
        public Message GetLastMessage(bool avoidNarrator = false)
        {
            Message Message = new Message();
            if (Messages.Count > 0)
            {
                Message = Messages.Last();
                if (avoidNarrator && Message.Owner.Name == "Narrator" && Messages.Count > 2)
                {
                    Message = Messages[Messages.Count - 2];
                }
            }

            return Message;
        }
        public async Task<Message> StreamLLMEditingMessage(string newtoken, Guid guid)
        {
            Message? message = null;

            foreach (var item in Messages)
            {
                if (item.GuidMessage == guid)
                {
                    message = item;
                    break;
                }
            }
            if (message == null)
            {
                return null;
            }
            message.isGenerating = true;
            message.Content += newtoken;
            return message;
            await Task.CompletedTask;
        }

    }
}
