using MousyHub.Models.Misc;

namespace MousyHub.Models.Model
{
    public class GenerationConfig
    {
        public bool MousyHubRecommended { get; set; } = false;
        public string ConfigName { get; set; }
        public double temp { get; set; } = 0.8f;
        public double rep_pen { get; set; } = 1.1f;
        public int rep_pen_range { get; set; } = 64;
        public double top_p { get; set; } = 0.95f;
        public double min_p { get; set; } = 0.05f;
        public double top_a { get; set; }
        public int top_k { get; set; } = 40;
        public double typical { get; set; } = 1f;
        public double tfs { get; set; } = 1f;
        public double rep_pen_slope { get; set; }
        public int[] sampler_order { get; set; }
        public string[] stop_sequence { get; set; }
        public int mirostat { get; set; }
        public double mirostat_tau { get; set; } = 5f;
        public double mirostat_eta { get; set; } = 0.1f;

        public bool dynatemp { get; set; } = false;

        public double min_temp { get; set; } = 0;
        public double max_temp { get; set; } = 2;
        public double dynatemp_exponent { get; set; } = 1;

        public int dry_allowed_length { get; set; } = 2;
        public double dry_multiplier { get; set; } = 0;
        public double dry_base { get; set; } = 1.75;
        public int dry_penalty_last_n { get; set; } = 0;

        public string dry_sequence_breakers_string { get; set; } = "\"\\n\", \":\", \"\\\"\", \"*\"";



        public bool use_default_badwordsids { get; set; }
        public string grammar { get; set; }


        public void CloneInList(List<GenerationConfig> presets)
        {
            var name = ConfigName;
            while (presets.Any(x => x.ConfigName == name))
            {
                name += " - cloned";
            }
            presets.Add((GenerationConfig)Util.CloneObject(this));
            ConfigName = name;
        }
        public class DrySampler
        {

        }

    }
}
