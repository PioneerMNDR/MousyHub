using MousyHub.Components.Models.Misc;

namespace MousyHub.Components.Models.Model
{
    public class Instruct
    {
        public string system_prompt { get; set; }
        public string input_sequence { get; set; }
        public string output_sequence { get; set; }
        public string last_output_sequence { get; set; }
        public string system_sequence { get; set; }
        public string stop_sequence { get; set; }
        public bool wrap { get; set; }
        public bool macro { get; set; }
        public bool names { get; set; }
        public bool names_force_groups { get; set; }
        public string activation_regex { get; set; }
        public string system_sequence_prefix { get; set; }
        public string system_sequence_suffix { get; set; }
        public string first_output_sequence { get; set; }
        public bool skip_examples { get; set; }
        public string output_suffix { get; set; }
        public string input_suffix { get; set; }
        public string system_suffix { get; set; }
        public string user_alignment_message { get; set; }
        public bool system_same_as_user { get; set; }
        public string last_system_sequence { get; set; }
        public string name { get; set; }

        public bool forWizard { get; set; } = false;

        public void CloneInList(List<Instruct> instructs)
        {
            var name = this.name;
            while (instructs.Any(x => x.name == name))
            {
                name += " - cloned";
            }
            instructs.Add((Instruct)Util.CloneObject(this));
            this.name = name;
           
        }

    }
}
