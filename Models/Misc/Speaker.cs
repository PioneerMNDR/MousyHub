using MousyHub.Models;
using Newtonsoft.Json;

namespace MousyHub.Models.Misc
{
    public class Speaker
    {
        public Speaker()
        {

        }
        public Speaker(int order, Person person, bool isActive, bool isWriting = false)
        {
            Order = order;
            Person = person;
            isAction = isActive;
            this.isWriting = isWriting;
            Id = person.Id;
        }

        public int Order { get; set; } = 0;

        public int SkipNow { get; set; } = 0;
        public int SkipCount { get; set; } = 0;

        public string Id { get; set; }
        [JsonIgnore]
        public Person Person { get; set; }

        public bool isAction { get; set; } = false;

        public bool isWriting { get; set; } = false;




    }
}
