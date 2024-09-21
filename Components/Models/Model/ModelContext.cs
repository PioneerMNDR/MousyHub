namespace MousyHub.Components.Models.Model
{
    public class ModelContext
    {
        public string StoryString { get; set; }
        public string ExampleSeparator { get; set; }
        public string ChatStart { get; set; }
        public bool UseStopStrings { get; set; }
        public bool AllowJailbreak { get; set; }
        public bool AlwaysForceName2 { get; set; }
        public bool TrimSentences { get; set; }
        public bool IncludeNewline { get; set; }
        public bool SingleLine { get; set; }
        public string Name { get; set; }
    }
}
