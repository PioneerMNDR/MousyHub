namespace LLMRP.Components.Models
{
    public class CharCard
    {
        public Data data { get; set; }
        public string spec { get; set; }
        public string spec_version { get; set; }
        public string char_persona { get; set; }
        public string avatar { get; set; }
        public DateTime date { get; set; }
        public byte[] avatarPNG { get; set; }
        public bool isNew { get; set; }

        public class Data
        {
            public string[] alternate_greetings { get; set; }
            public string avatar { get; set; }
            public object character_book { get; set; }
            public string character_version { get; set; }
            public string creator { get; set; }
            public string creator_notes { get; set; }
            public string description { get; set; }
            public Extensions extensions { get; set; }


            public string first_mes { get; set; }
            public string mes_example { get; set; }
            public string name { get; set; }
            public string personality { get; set; }
            public string post_history_instructions { get; set; }
            public string scenario { get; set; }

            public string short_description { get; set; }
            public string system_prompt { get; set; }
            public string[] tags { get; set; }
        }

        public class Chub
        {
            public string full_path { get; set; }
            public int id { get; set; }


        }

        public class Extensions
        {
            public Chub chub { get; set; }
            public int depth { get; set; }
            public string prompt { get; set; }
            public bool fav { get; set; }
            public string talkativeness { get; set; }
            public string world { get; set; }

            public class AltExpressions
            {
            }
        }
    }
}