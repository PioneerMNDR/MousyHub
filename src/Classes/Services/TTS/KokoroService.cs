using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using KokoroSharp.Utilities;
using MousyHub.Classes.Misc;

namespace MousyHub.Classes.Services.TTS
{
    public class KokoroService
    {
        public KokoroService(IHostEnvironment environment) 
        {
            _environment = environment;
            if (_environment.IsDevelopment())
            {
                Tokenizer.eSpeakNGPath = Path.Combine(AppContext.BaseDirectory, "espeak");
                KokoroVoiceManager.LoadVoicesFromPath(Path.Combine(AppContext.BaseDirectory, "voices"));
            }
        }
        private IHostEnvironment _environment;
        private KokoroWavSynthesizer? TTS { get; set; }
        public bool IsRun { get; private set; }

        public void TryRunModel(string modelPath)
        {
            try
            {
                if (IsRun && TTS!=null)
                {
                    return;
                }
                TTS = new KokoroWavSynthesizer(modelPath);
       
                IsRun = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed run kokoro model: " + ex.Message);
                IsRun = false;
            }
        }

        public async Task<byte[]> GetSpeak(string text, string KokoroVoice)
        {
            if (IsRun) 
            {
                text = StringHelperBuilder.RemoveAsterisks(text);
                var voice = KokoroVoiceManager.GetVoice(KokoroVoice);
                var bytes = await TTS.SynthesizeAsync(text, voice);
              
                return bytes;
            }
            return Array.Empty<byte>();       
        }

        public List<KokoroVoice> GetVoices()
        {
            return KokoroVoiceManager.Voices;
        }

    }
}
