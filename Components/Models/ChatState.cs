using MousyHub.Components.Models.Model;
using MousyHub.Components.Models.Services;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MousyHub.Components.Models
{
    public class ChatState : IDisposable
    {
        public ChatHistory? ChatHistory { get; set; }
        private UploaderService UploaderService { get; set; }
        private SettingsService Settings { get; set; }
        private ProviderService Provider { get; set; }

        private TranslatorService TranslatorService { get; set; }

        public List<Person> AllPersons { get; set; } = new List<Person>();

        public Person NextPerson;



        public ChatState(UploaderService uploaderService, SettingsService settingsService, ProviderService Provider, TranslatorService translatorService)
        {
            this.UploaderService = uploaderService;
            this.Settings = settingsService;
            this.Provider = Provider;
            AddPerson(Provider.Wizard.Narrator);
            if (settingsService.CurrentUserProfile != null)
            {
                AddPerson(settingsService.CurrentUserProfile);
            }
            UploaderService.SaveInfoEvent += SaveChatHistory;
            TranslatorService = translatorService;
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
        }

        public async Task SetContextSize()
        {
            if (ChatHistory != null)
            {
                ChatHistory.ChatContextSize = await Provider.TokenCount(await ChatHistory.PromtToLLM(Settings.CurrentInstruct, ChatHistory.MainCharacter, false));
            }
        }



        //Summarize the chat and write the summarization result to Chat History.Summarized Context
        public async Task<bool> ChatSummarize()
        {
            string preparePromt = "";
            List<Message> messages = new List<Message>();
            if (ChatHistory.SummarizeContext == null)
            {
                preparePromt += PromtBuilder.SystemMessageShort(ChatHistory);
            }
            else
            {
                preparePromt += "Last summary(use this for summarize too): " + ChatHistory.SummarizeContext;
            }
            foreach (var item in ChatHistory.Messages)
            {
                if (item.isSummarized == false && item != ChatHistory.GetLastMessage())
                {
                    preparePromt += "\n" + item.Owner.Name + ": " + item.Content;
                    messages.Add(item);
                }
            }
            var res = await Provider.Wizard.WizardRequest(preparePromt, Misc.Wizard.WizardFunction.Summary);
            if (res.IsSuccess)
            {
                ChatHistory.SummarizeContext = res.Content;
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
        //Prepare prompt for quick answers and return the result
        public async Task<string> AnswerAssistant()
        {
            string preparePromt = "";
            preparePromt += PromtBuilder.SystemMessageShort(ChatHistory);
            preparePromt += "[DIALOGUE]: ";
            //Take only 4 last message
            foreach (var item in ChatHistory.Messages.TakeLast(4))
            {
               preparePromt += "\n" + item.Owner.Name + ": " + item.Content;
            }
            preparePromt += "[END OF DIALOGUE]";
            var res = await Provider.Wizard.WizardRequest(preparePromt, Misc.Wizard.WizardFunction.AnswerAssistant, ChatHistory.MainUser.Name, ChatHistory.GetLastMessage(true).Owner.Name);
            if (res.IsSuccess)
            {
                Console.WriteLine(res.Content);
                var c = await TranslatorService.TranslateForUser(res.Content);
                Console.WriteLine(c);
                return c;
            }
            else
            {
                return "";
            }
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
