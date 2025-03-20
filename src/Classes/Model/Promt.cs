using Google.Protobuf.WellKnownTypes;
using Microsoft.IdentityModel.Tokens;
using Microsoft.KernelMemory.DataFormats;
using MousyHub.Classes.Misc;
using MousyHub.Models;
using MousyHub.Models.Model;

namespace MousyHub.Classes.Model
{
    public class Promt
    {
        static string separator = "\n";

        public Promt(ChatHistory chatHistory, Instruct instruct, Person person, bool isChat)
        {
            this.isChat = isChat;
            Elements = new List<PromtElement>();
            FullContent = string.Empty;         
            Build(chatHistory, instruct, person);
        }

        public Promt(string SystemPromt, string UserPromt, bool isChat)
        {
            this.isChat = isChat;
            Elements = new List<PromtElement>();
            Elements.Add(new PromtElement(MessageRole.System, SystemPromt));
            Elements.Add(new PromtElement(MessageRole.User, UserPromt));
            FullContent = SystemPromt + separator + UserPromt;
        }

        public List<PromtElement> Elements { get; private set; }
        public string FullContent { get; private set; }
        /// <summary>
        /// Parameter that determines whether Chat completions or text completions will be used in Cloud services
        /// </summary>
        public bool isChat { get; set; } = false;

        private void Build(ChatHistory chatHistory, Instruct instruct, Person person)
        {                          
            Elements.Add(new PromtElement(MessageRole.System, SystemMessage(instruct, chatHistory, person,false)));
            FullContent = SystemMessage(instruct, chatHistory, person, true);
            if (!string.IsNullOrEmpty(chatHistory.SummarizeContext))
            {
                Elements.Add(new PromtElement(MessageRole.System, GetSummarizePromt(chatHistory.SummarizeContext)));
                FullContent += GetSummarizePromt(chatHistory.SummarizeContext);
            }
            //For narrator and RAG
            AddAdditionalContent(chatHistory, person);
            string FullDialogue = string.Empty;
            foreach (var item in chatHistory.Messages)
            {
                if (item.Owner.IsUser && item.isSummarized == false)
                {
                    Elements.Add(new PromtElement(MessageRole.User, $"{item.Content}"));
                    FullDialogue += item.InstructContent;
                }
                else if (item.isSummarized == false)
                {
    
                        if (person == chatHistory.MainCharacter)
                            Elements.Add(new PromtElement(MessageRole.Assistant, $"{item.Content}"));
                        else
                            Elements.Add(new PromtElement(MessageRole.Assistant, $"{item.Owner.Name}: {item.Content}"));
            
                      //если меняется istruct то здесь ничего не поменется требуется решение
                    FullDialogue += item.InstructContent;
                    FullDialogue += item.Content;
                }
               
            }
            if (!string.IsNullOrEmpty(instruct.jailbreak_promt))
            {
                Elements.Add(new PromtElement(MessageRole.System, instruct.jailbreak_promt));
            }
          
            FullContent += FullDialogue;
        }

        private string SystemMessage(Instruct instruct, ChatHistory chatHistory, Person person, bool IsInstructed)
        {
            string system = "";
            string story_string = "";      
            if (IsInstructed)
            {
                system = string.IsNullOrEmpty(person.OverrideSystemPromt)
                    ? instruct.system_sequence + separator + instruct.system_prompt
                    : instruct.system_sequence + separator + person.OverrideSystemPromt;
            }
            else
            {
                system = string.IsNullOrEmpty(person.OverrideSystemPromt)
                    ? instruct.system_prompt
                    : person.OverrideSystemPromt;
            }
            if (person.Id == chatHistory.MainCharacter.Id)
            {
                story_string = system + separator + "{{Char}}\' s description:" + chatHistory.CharDesription + separator;
                if (!string.IsNullOrEmpty(chatHistory.Mes_Example))
                    story_string += "{{Char}}\' s message examples:" + chatHistory.Mes_Example + separator;
                if (!string.IsNullOrEmpty(chatHistory.Personality))
                    story_string += "{{Char}}\' s personality:" + chatHistory.Personality + separator;
                if (!string.IsNullOrEmpty(chatHistory.Scenario))
                    story_string += "Scenario:" + chatHistory.Scenario + separator;
            }
            else
            {
                story_string = system + separator + $"{person.Name}'s description:" + person.Description  + separator;
                if (!string.IsNullOrEmpty(chatHistory.Scenario))
                    story_string += "Scenario:" + chatHistory.Scenario + separator;
                if (!string.IsNullOrEmpty(chatHistory.CharShortDesription))
                    story_string += "{{Char}}'s short description::" + chatHistory.CharShortDesription + separator;
            }
                
            story_string = StringHelperBuilder.TagPlaceholder(story_string, chatHistory.MainUser.Name, chatHistory.MainCharacter.Name);
            return story_string;
        }

        private string GetSummarizePromt(string value)
        {
            return $"{separator}The summary the events in dialogue:{value}";
        }
        private void AddAdditionalContent(ChatHistory chatHistory, Person person)
        {
            string additionalpromt =string.Empty;
            //Special condition for the narrator
            if (person.Name == "Narrator" && !string.IsNullOrEmpty(chatHistory.PlayerWishes))
            {
                additionalpromt += "\n[Player's wishes: " + chatHistory.PlayerWishes + "]";
            }
            //Memory From RAG
            if (!string.IsNullOrEmpty(chatHistory.MemoryFromChat))
            {
                additionalpromt += "\n[Early Memories from Chat (Possibly for use): {" + chatHistory.MemoryFromChat + "}]";
                //chatHistory.MemoryFromChat = string.Empty;
            }
            if (!string.IsNullOrEmpty(additionalpromt))
            {
                Elements.Add(new PromtElement(MessageRole.System, additionalpromt));
            }
            FullContent += additionalpromt;

        }

        public enum MessageRole
        {
            System,
            Assistant,
            User
        }
        public class PromtElement
        {
            public PromtElement(MessageRole messageRole, string content)
            {
                MessageRole = messageRole;
                Content = content;
            }

            public MessageRole MessageRole { get; set; } = MessageRole.System;
            public string Content { get; set; }
        }

    }
}
