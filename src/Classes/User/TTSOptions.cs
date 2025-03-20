namespace MousyHub.Classes.User
{
    public class TTSOptions
    {
        public bool Enabled { get; set; } =false;
        public float PlaybackSpeed { get; set; } = 1f;
        public float VolumeValue { get; set; } = 1f;

        public bool  TwoSentencesMode { get; set; } = true;
        public bool SplitVoice { get; set; } = true;
        public bool UseCapitalLetterAsBreak { get; set; } = false;
        public string NarratorKokoroVoice { get; set; } = "am_michael";
        public string UserKokoroVoice { get; set; } = "am_fenrir";

    }
}
