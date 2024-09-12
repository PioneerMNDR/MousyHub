namespace LLMRP.Components.Models.Misc
{
    public class Speaker
    {
        public Speaker(int order, Person person,bool isActive,bool isWriting= false)
        {
            Order = order;
            Person = person;
            isAction = isActive;
            this.isWriting = isWriting;
        }

        public int Order { get; set; } = 0;

        public int SkipNow { get; set; } = 0;
        public int SkipCount { get; set; } = 0;
        public Person Person { get; set; } = new Person();

        public bool isAction { get; set; } = false;

        public bool isWriting { get; set; } = false;




    }
}
