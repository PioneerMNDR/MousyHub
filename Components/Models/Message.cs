using MousyHub.Components.Models.Services;
using Newtonsoft.Json;

namespace MousyHub.Components.Models
{
    public class Message
    {
        public Message()
        {

        }
        public Message(string content, Person owner)
        {
            Content = content;
            Owner = owner;
            dateTime=DateTime.Now;
            dateTimeString = dateTime.ToString("dd MMMM, yyyy HH:mm");
            IdOwner = owner.Id;
        }
        public Message(string content,string InstructContent, Person owner)
        {
            Content = content;
            this.InstructContent = InstructContent;
            Owner = owner;
            dateTime = DateTime.Now;
            dateTimeString = dateTime.ToString("dd MMMM, yyyy HH:mm");
            IdOwner = owner.Id;
        }

        public async Task TranslateMessage(TranslatorService translatorService)
        {
            if (translatorService.isEnabled)
            {
                UserNativeLanguageContent = await translatorService.TranslateForUser(Content);
            }
          
        }

        public string Content { get; set; }

        public string UserNativeLanguageContent { get; set; }

        public string InstructContent { get; set; }
            
        public DateTime dateTime{ get; set; }
        public bool isNewMessage { get; set; } = true;
        public bool isGenerating{ get; set; } = false;

        public bool isSummarized { get; set; } = false;

        public string dateTimeString { get; set; }
        [JsonIgnore]
        public Person Owner { get; set; }
        public Guid GuidMessage { get; set; } =  Guid.NewGuid();
        public string IdOwner { get; set; }

    }
}
