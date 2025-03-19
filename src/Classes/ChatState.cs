using Microsoft.CodeAnalysis.CSharp.Syntax;
using MousyHub.Models.Services;
using MousyHub.Classes.Misc;

namespace MousyHub.Models
{
    public class ChatState : IDisposable
    {
        public ChatHistory? ChatHistory { get; set; }
        private UploaderService UploaderService { get; set; }
        private SettingsService Settings { get; set; }
        private ProviderService Provider { get; set; }

        private RAGService RAGService { get; set; }
        private TranslatorService TranslatorService { get; set; }

        public List<Person> AllPersons { get; set; } = new List<Person>();

        public Person NextPerson;
        public delegate Task TaskDelegate();

        public event TaskDelegate StartGenerationEvent;
        public event TaskDelegate EndGenerationEvent;



        public ChatState(UploaderService uploaderService, SettingsService settingsService, ProviderService Provider, TranslatorService translatorService, RAGService RAG)
        {
            UploaderService = uploaderService;
            Settings = settingsService;
            this.Provider = Provider;
            AddPerson(Provider.Wizard.Narrator);
            if (settingsService.CurrentUserProfile != null)
            {
                AddPerson(settingsService.CurrentUserProfile);
            }
            UploaderService.SaveInfoEvent += SaveChatHistory;
            TranslatorService = translatorService;
            RAGService = RAG;
        }
        public async Task CheckChatHistory(CharCard charCard)
        {
            ChatHistory = UploaderService.LoadChatHistory(charCard);

            if (ChatHistory == null)
            {
                await NewChatHistory(charCard);
            }
            else
            {
                ChatHistory.ChatLoading(Settings.CurrentUserProfile, TakePerson(charCard), AllPersons);
                if (ChatHistory.TotalMessagesCount>5)
                {
                    await ExportChatToMemory();
                }
            }
            await SetContextSize();
        }

        public async Task NewChatHistory(CharCard charCard)
        {
            ChatHistory newchat = new ChatHistory(Settings.CurrentUserProfile, TakePerson(charCard), AllPersons);
            ChatHistory = newchat;
            ChatHistory.AddToQueue(AllPersons.Where(x => x.Name == "Narrator").FirstOrDefault(), false);
            //FirstMessage
            var mes = await ChatHistory.AddMessage(ChatHistory.FirstMessage, ChatHistory.MainCharacter, Settings.CurrentInstruct);
            await mes.TranslateMessage(TranslatorService);
            //AltMessages
            ChatHistory.FillAltFirstMessagesList(ChatHistory.MainCharacter, Settings.CurrentInstruct);
        }
        public async Task ClearAndNewChatHistory()
        {
            CharCard charCard = ChatHistory.MainCharacter.CharacterCard;
            ChatHistory newchat = new ChatHistory(Settings.CurrentUserProfile, TakePerson(charCard), AllPersons);
            ChatHistory = newchat;
            ChatHistory.AddToQueue(AllPersons.Where(x => x.Name == "Narrator").FirstOrDefault(), false);
            //FirstMessage
            var mes = await ChatHistory.AddMessage(ChatHistory.FirstMessage, ChatHistory.MainCharacter, Settings.CurrentInstruct);
            await mes.TranslateMessage(TranslatorService);
            //AltMessages
            ChatHistory.FillAltFirstMessagesList(ChatHistory.MainCharacter, Settings.CurrentInstruct);
            await SetContextSize();
            await RAGService.ClearMemories(ChatHistory.ChatName);
        }

        public async Task SetContextSize()
        {
            if (ChatHistory != null)
            {
                ChatHistory.ChatContextSize = await Provider.TokenCount(ChatHistory.GetPromt(Settings.CurrentInstruct, ChatHistory.MainCharacter,false).FullContent);
            }
        }



        //Summarize the chat and write the summarization result to Chat History.Summarized Context
        //*It is necessary to move the method to another class
        public async Task<bool> ChatSummarize()
        {
            string preparePromt = "";
            List<Message> messages = new List<Message>();
            if (string.IsNullOrEmpty(ChatHistory.SummarizeContext))
            {
                preparePromt += StringHelperBuilder.SystemMessageShort(ChatHistory);
            }
            else
            {
                preparePromt += $"Last summary(use this for summarize too): [{ChatHistory.SummarizeContext}]\n";
            }
            preparePromt += "<ACTUAL DIALOG>";
            foreach (var item in ChatHistory.Messages)
            {
           
                if (item.isSummarized == false && item != ChatHistory.GetLastMessage() && item != ChatHistory.GetLastMessage(offset:1))
                {
                    preparePromt += "\n" + item.Owner.Name + ": " + item.Content;
                    messages.Add(item);
                }
            }
            preparePromt += "<END>";
            var res = await Provider.Wizard.WizardRequest(preparePromt, Misc.Wizard.WizardFunction.Summary,MaxTokens:300, Temperature: 0.5);
         
            if (res.IsSuccess)
            {
                ChatHistory.SummarizeContext = res.Content.TrimStart('\n');
                foreach (var item in messages)
                {
                    item.isSummarized = true;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public async Task ExportChatToMemory()
        {
            if (Settings.User.RAGOptions.Enabled && RAGService.IsAvailable)
            {
                string BodyRequest = "";
                foreach (var item in ChatHistory.Messages)
                {
                    if (item != ChatHistory.GetLastMessage())
                    {
                        BodyRequest += "\n" + item.Owner.Name + ": " + item.Content;
                    }
                }
               await RAGService.ImportMemory(BodyRequest, ChatHistory.ChatName);
            }
        }

        //Prepare prompt for quick answers and return the result
        //*It is necessary to move the method to another class
        public async Task<string> AnswerAssistant()
        {
            string preparePromt = "";
            preparePromt += StringHelperBuilder.SystemMessageShort(ChatHistory);
            preparePromt += "[DIALOGUE]: ";
            //Take only 4 last message
            foreach (var item in ChatHistory.Messages.TakeLast(4))
            {
                preparePromt += "\n" + item.Owner.Name + ": " + item.Content;
            }
            preparePromt += "[END OF DIALOGUE]";
            var res = await Provider.Wizard.WizardRequest(preparePromt, Misc.Wizard.WizardFunction.AnswerAssistant,UserName: ChatHistory.MainUser.Name,CharName: ChatHistory.GetLastMessage(true).Owner.Name,Temperature:0.5);
            if (res.IsSuccess)
            {
                Console.WriteLine(res.Content);
                string response = res.Content;
                if (Settings.User.TranslatorOptions.isEnabled)
                {
                     response = await TranslatorService.TranslateForUser(res.Content);
                }

                Console.WriteLine(response);
                return response;
            }
            else
            {
                return "";
            }
        }
        
        public async Task StartGenerationEventRun()
        {
            StartGenerationEvent?.Invoke();
        }
        public async Task EndGenerationEventRun()
        {
            EndGenerationEvent?.Invoke();
        }
        public void AddPerson(Person person)
        {
            if (AllPersons.Contains(person) == false)
            {
                AllPersons.Add(person);
            }
        }
        public void AddPerson(CharCard card)
        {
            if (AllPersons.Any(x => x.CharacterCard != null && x.CharacterCard.data.name == card.data.name) == false)
            {
                Person person = new Person(card, "main");
                AllPersons.Add(person);
            }
        }

        public Person TakePerson(Person Old_person)
        {
            Person person = AllPersons.Where(x => x.Name == Old_person.Name).FirstOrDefault(Old_person);
            return person;
        }

        public Person TakePerson(CharCard card)
        {
            Person new_person = new Person(card, "main");
            Person person = AllPersons.Where(x => x.CharacterCard != null && x.CharacterCard.data.name == card.data.name).FirstOrDefault(new_person);
            return person;

        }
        public void RemovePerson(CharCard card)
        {
            Person? person = AllPersons.Where(x => x.CharacterCard != null && x.CharacterCard.data.name == card.data.name).FirstOrDefault();
            if (person != null) 
            {
                AllPersons.Remove(person);  
            }
           
        }

        public void SaveChatHistory(object? sender, EventArgs e)
        {
            if (ChatHistory != null)
            {
                Saver.SaveChatHistory(ChatHistory);
            }

        }

        public void Dispose()
        {
            UploaderService.SaveInfoEvent -= SaveChatHistory;
        }


    }
}
