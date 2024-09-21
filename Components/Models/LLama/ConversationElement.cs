using LLama.Batched;
using LLama.Native;

namespace MousyHub.Components.Models.LLama
{
    public class ConversationElement
    {
        
        public Conversation Conversation { get; set; }
        public List<LLamaToken> _tokens = [];
        public int Priority = 0;

        public ConversationElement(Conversation conversation, string id)
        {
            Conversation = conversation;
            Id = id;

        }

        public string Id { get; set; }


    }
}
