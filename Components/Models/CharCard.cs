using System.Text.RegularExpressions;

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
        public string system_name { get 
            {
                string fileName = data.creator + "_" + data.name;
                string regSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                Regex rg = new Regex(string.Format("[{0}]", Regex.Escape(regSearch)));
                fileName = rg.Replace(fileName, "");
                return fileName;
            }
            private set { }
        }

        public class Data
        {
            public string[] alternate_greetings { get; set; }
            public string avatar { get; set; }
            public object character_book { get; set; }
            public CharacterBook characterBook { get; set; } // novo
            public string character_version { get; set; }
            public string creator { get; set; }
            public string creator_notes { get; set; }
            public string description { get; set; }
            public Extensions extensions { get; set; }
            public Dictionary<string, object> extensionsDictionary { get; set; } // novo
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
        }

        public class CharacterBook
        {
            public string name { get; set; }
            public string description { get; set; }
            public int? scan_depth { get; set; }
            public int? token_budget { get; set; }
            public bool? recursive_scanning { get; set; }
            public Dictionary<string, object> extensions { get; set; }
            public Entry[] entries { get; set; }
        }

        public class Entry
        {
            public string[] keys { get; set; }
            public string content { get; set; }
            public Dictionary<string, object> extensions { get; set; }
            public bool enabled { get; set; }
            public int insertion_order { get; set; }
            public bool? case_sensitive { get; set; }
            public string name { get; set; }
            public int? priority { get; set; }
            public int? id { get; set; }
            public string comment { get; set; }
            public bool? selective { get; set; }
            public string[] secondary_keys { get; set; }
            public bool? constant { get; set; }
            public string position { get; set; } // 'before_char' | 'after_char'
        }
    }
}