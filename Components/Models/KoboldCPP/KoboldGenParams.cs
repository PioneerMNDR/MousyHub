using Newtonsoft.Json;

namespace LLMRP.Components.Models.KoboldCPP
{
    public class KoboldGenParams
    {
        [JsonProperty("prompt")]
        public string Prompt { get; set; }
        [JsonProperty("n")]
        public int N { get; set; }
        [JsonProperty("max_context_length")]
        public int MaxContextLength { get; set; }
        [JsonProperty("max_length")]
        public int MaxLength { get; set; }
        [JsonProperty("rep_pen")]
        public float RepPen { get; set; }
        [JsonProperty("temperature")]
        public float Temperature { get; set; }
        [JsonProperty("temp")]
        public float TemperatureAlt { set { Temperature = value; } }
        [JsonProperty("top_p")]
        public float TopP { get; set; }
        [JsonProperty("top_k")]
        public int TopK { get; set; }
        [JsonProperty("top_a")]
        public float TopA { get; set; }
        [JsonProperty("typical")]
        public float Typical { get; set; }
        [JsonProperty("tfs")]
        public float Tfs { get; set; }
        [JsonProperty("trim_stop")]
        public bool trim_stop { get; set; } = true;
        [JsonProperty("rep_pen_range")]
        public int RepPenRange { get; set; }

        [JsonProperty("sample_order")]
        public List<int> SamplerOrder { get; set; }
        [JsonProperty("quiet")]
        public bool Quiet { get; set; }
        [JsonProperty("genkey")]

        public string genkey { get; set; }
        [JsonProperty("stop_sequence")]
        public string[] stop_sequence { get; set; }

        public float dynatemp_range { get; set; } = 0;
        [JsonProperty("dynatemp_exponent")]
        public float dynatemp_exponent { get; set; } = 1;
        [JsonProperty("dry_allowed_length")]
        public int dry_allowed_length { get; set; } = 2;
        [JsonProperty("dry_multiplier")]
        public float dry_multiplier { get; set; } = 0.8f;
        [JsonProperty("dry_base")]
        public float dry_base { get; set; } = 1.75f;
        [JsonProperty("dry_penalty_last_n")]
        public int dry_penalty_last_n { get; set; } = 0;

        public string[] dry_sequence_breakers { get; set; } = { "\n", ":", "\"", "*" };

        public KoboldGenParams(string prompt = "", int n = 1, int maxContextLength = 2048,
            int maxLength = 80, float repPen = 1.1f, float temperature = 0.59f,
            float topP = 1f, int topK = 0, float topA = 0f, float typical = 1f, float tfs = 0.87f,
            int repPenRange = 2048, List<int> samplerOrder = null, bool quiet = true, string genkey = "main",
            float dynatemp_exponent = 1,
            int dry_allowed_length = 2, float dry_multiplier = 0.8f, float dry_base = 1.75f, float dry_penalty_last_n = 0)
        {
            Prompt = prompt;
            N = n;
            MaxContextLength = maxContextLength;
            MaxLength = maxLength;
            RepPen = repPen;
            TopP = topP;
            TopK = topK;
            TopA = topA;
            Typical = typical;
            Tfs = tfs;
            RepPenRange = repPenRange;
            SamplerOrder = samplerOrder ?? new List<int> { 5, 0, 2, 3, 1, 4, 6 };
            Quiet = quiet;
            this.genkey = genkey;
            this.dynatemp_exponent = dynatemp_exponent;
            this.dry_allowed_length = dry_allowed_length;
            this.dry_multiplier = dry_multiplier;
            this.dry_base = dry_base;
        }

        public void SetDynTemp(bool dynatempEnabled = false, float min_temp = 0, float max_temp = 2)
        {
            Temperature = dynatempEnabled ? ((min_temp + max_temp) / 2) : Temperature;
            dynatemp_range = dynatempEnabled ? ((max_temp - min_temp) / 2) : 0;
        }




        public string GetJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
