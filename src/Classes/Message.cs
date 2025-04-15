using MousyHub.Classes.User;
using MousyHub.Models.Services;
using Newtonsoft.Json;
using System.Diagnostics;

namespace MousyHub.Models
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
            dateTime = DateTime.Now;
            dateTimeString = dateTime.ToString("dd MMMM, yyyy HH:mm");
            IdOwner = owner.Id;
        }
        public Message(string content, string InstructContent, Person owner)
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

        //// Новый метод для обработки и разделения контента
        //public void ProcessReasoningContent(ReasoningOptions options)
        //{
        //    options ??= new ReasoningOptions();

        //    if (Content.Contains(options.Suffix))
        //    {
        //        int endThinkIndex = Content.IndexOf(options.Suffix);

        //        // Извлекаем reasoning контент (всё до суффикса)
        //        ReasoningContent = Content.Substring(0, endThinkIndex);

        //        // Обновляем основной контент (всё после суффикса)
        //        Content = Content.Substring(endThinkIndex + options.Suffix.Length);
        //    }
        //    else
        //    {
        //        // Если маркер не найден, считаем всё reasoning контентом
        //        ReasoningContent = Content;
        //        Content = "";
        //    }
        //}

        //// Метод для добавления нового контента с обработкой reasoning
        //public void AppendContent(string newContent, ReasoningOptions options)
        //{
        //    Debug.Write(newContent);
        //    string updatedContent = Content + newContent;

        //    // Если уже найден маркер reasoning в предыдущем контенте
        //    if (Content.Contains(options.Suffix))
        //    {
        //        // Просто добавляем к обычному контенту
        //        Content += newContent;
        //    }
        //    // Если маркер найден в обновленном контенте (впервые)
        //    else if (updatedContent.Contains(options.Suffix))
        //    {
        //        int endThinkIndex = updatedContent.IndexOf(options.Suffix);

        //        // Извлекаем reasoning контент
        //        ReasoningContent = updatedContent.Substring(0, endThinkIndex);

        //        // Обновляем основной контент
        //        Content = updatedContent.Substring(endThinkIndex + options.Suffix.Length);
        //    }
        //    // Если маркер еще не найден
        //    else
        //    {
        //        // Добавляем к reasoning контенту
        //        ReasoningContent += newContent;
        //        Content = "";
        //    }
        //}

        public string Content { get; set; }

        public string ReasoningContent { get; set; }

        public string UserNativeLanguageContent { get; set; }

        //if user <start>user<end>Hello<end><botstart>
        public string InstructContent { get; set; }

        public DateTime dateTime { get; set; }

        public bool isGenerating { get; set; } = false;

        public bool isSummarized { get; set; } = false;

        public string dateTimeString { get; set; }
        [JsonIgnore]
        public Person Owner { get; set; }
        public Guid GuidMessage { get; set; } = Guid.NewGuid();
        public string IdOwner { get; set; }

    }
}
