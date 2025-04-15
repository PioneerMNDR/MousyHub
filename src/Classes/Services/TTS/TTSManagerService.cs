using MousyHub.Classes.Misc;
using MousyHub.Models;
using MousyHub.Models.Services;
using System.Diagnostics;


namespace MousyHub.Classes.Services.TTS
{
    public class TTSManagerService
    {
        private readonly KokoroService _kokoroService;
        private readonly AudioService _audioService;
        private readonly SettingsService _settingsService;

        public TTSManagerService(KokoroService kokoroService, AudioService audioService, SettingsService settingsService)
        {
            _kokoroService = kokoroService;
            _audioService = audioService;
            _settingsService = settingsService;
        }
        public bool ReadyToUse
        {
            get
            {
                return _kokoroService.IsRun;
            }
        }


        private CancellationTokenSource _processingCts = new CancellationTokenSource();
        private List<KokoroSentence> BufferSentences = new List<KokoroSentence>();
        public bool IsQueued=false;
        private string Buffer { get; set; }

        bool NarratorSequence = false;
        public async Task InterruptProcessingAsync()
        {
            if (_settingsService.User.TTSOptions.Enabled)
            {
                // Cancel the current processing
                if (!_processingCts.IsCancellationRequested)
                {
                    _processingCts.Cancel();

                    // Create a new cancellation token source for future operations
                    _processingCts = new CancellationTokenSource();
                }


                Buffer = string.Empty;
                BufferSentences.Clear();

                NarratorSequence = false;

                // Clear audio queue
                await _audioService.StopAllAsync();
                await _audioService.ClearQueueAsync();
                    IsQueued = false;
                Debug.WriteLine("Text processing interrupted");
            }
        }


        public async Task ProcessStreamingText(string newText, string char_voice_name, Person? person = null)
        {
            if (_settingsService.User.TTSOptions.Enabled == false)
                return;

            try
            {
                CancellationToken token = _processingCts.Token;
          
                Buffer += newText;
                // Parse the text and get only new sentences
                var newSentences =  ParseTextAndGetNewSentences(Buffer, token);
                IsQueued = true;
                // Only speak these new sentences
                await SpeakSentencesList(newSentences, char_voice_name, token, person);
              
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Text processing interrupted");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while processing text: {ex.Message}");
            }
        }

        private List<KokoroSentence> ParseTextAndGetNewSentences(string text, CancellationToken token)
        {
            text = text.Replace("\r\n", " ").Replace("\n", " ");
            string[] AllSentences = text.SplitWithDefaultSeparators();

            var newSentences = new List<KokoroSentence>();
            foreach (string sent in AllSentences)
            {
                // Use a more robust way to check if this sentence already exists
                if (!BufferSentences.Any(x => x.Text.NormalizeString().Equals(sent.NormalizeString(), StringComparison.Ordinal)))
                {
                    var newSent = new KokoroSentence(sent, false);
                    if (newSent.textMarkerType is KokoroSentence.TextMarkerType.StartOnly)
                        NarratorSequence = true;
                    if (NarratorSequence && newSent.textMarkerType is KokoroSentence.TextMarkerType.None)
                        newSent.IsNarrator = true;
                    if (NarratorSequence && newSent.textMarkerType is KokoroSentence.TextMarkerType.EndOnly)
                        NarratorSequence = false;

                    BufferSentences.Add(newSent);
                    newSentences.Add(newSent);
                }
            }
            return newSentences;
        }
        private async Task SpeakSentencesList(List<KokoroSentence> sentencesToSpeak, string char_voice_name, CancellationToken token, Person? person = null)
        {
            foreach (var sentence in sentencesToSpeak)
            {
                // Check for cancellation without throwing an exception
                if (token.IsCancellationRequested)
                {                
                    break; 
                }            
                try
                {
                    byte[] wavData;
                    if (sentence.IsNarrator && _settingsService.User.TTSOptions.SplitVoice)
                    {
                        Console.WriteLine("Third person: " + sentence.Text);
                        wavData = await _kokoroService.GetSpeak(sentence.Text, _settingsService.User.TTSOptions.NarratorKokoroVoice);
                    }
                    else
                    {
                        Console.WriteLine("First person: " + sentence.Text);
                        wavData = await _kokoroService.GetSpeak(sentence.Text, char_voice_name);
                    }

                 
                    if (token.IsCancellationRequested)
                    {
                     
                        break;
                    }

                    sentence.Announce();
                    await _audioService.EnqueueAudioAsync(wavData, person: person);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                  
                    Console.WriteLine($"Processing error: {ex.Message}");
                }
            }
        }
        public async Task ProcessFullText(string fullText, string char_voice_name, Person? person = null)
        {
            if (_settingsService.User.TTSOptions.Enabled == false)
                return;

            await InterruptProcessingAsync();
            try
            {
                CancellationToken token = _processingCts.Token;
                NarratorSequence = false;
                BufferSentences.Clear();
                IsQueued = true;
                // Parse the full text and get all sentences (they're all new since we cleared the collection)
                var allSentences = ParseTextAndGetNewSentences(fullText, token);

                // Speak all sentences using our new method
                await SpeakSentencesList(allSentences, char_voice_name, token, person);
                IsQueued = false;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Text processing interrupted");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Processing error: {ex.Message}");
            }
        }

        public void EndSentenceThread()
        {
            if (_settingsService.User.TTSOptions.Enabled == false)
                return;

            try
            {
                // Clear everything
                NarratorSequence = false;
                if (BufferSentences.Count > 30)
                {
                    BufferSentences.Clear();
                }


                IsQueued = false;
                Buffer = string.Empty;

                Debug.WriteLine("The stream has been successfully completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Processing error: {ex.Message}");
            }
        }

    }

    public class KokoroSentence
    {
        public KokoroSentence(string text, bool isNarrator)
        {
            Text = text;
            IsNarrator = isNarrator;
            textMarkerType = DetectMarkerType(text);
            //Debug.WriteLine("|Sentence: " + Text);
            if (textMarkerType is TextMarkerType.BothEnds || textMarkerType is TextMarkerType.StartOnly || textMarkerType is TextMarkerType.EndOnly)
            {
                IsNarrator = true;
            }
        }
        public string Text { get; private set; }
        public bool IsNarrator { get; set; }
        public bool IsAnnounced { get; private set; } = false;

        public TextMarkerType textMarkerType { get; set; }
        public enum TextMarkerType
        {
            None, // No asterisks
            StartOnly, // Star only at the beginning
            EndOnly, // Star only at the end
            BothEnds // Stars at the beginning and at the end
        }



        public static TextMarkerType DetectMarkerType(string text)
        {
            if (string.IsNullOrEmpty(text))
                return TextMarkerType.None;

            string trimmedText = text.TrimStart();

            // Check if the text starts with a "*word*" pattern
            bool startsWithWordPattern = false;
            if (trimmedText.StartsWith("*"))
            {
                int nextAsteriskIndex = trimmedText.IndexOf('*', 1);
                if (nextAsteriskIndex > 1) // Make sure there's actual content between asterisks
                {
                    startsWithWordPattern = true;
                }
            }

            // If the text starts with a "*word*" pattern, it's not considered to start with an asterisk marker
            bool startsWithAsterisk = trimmedText.StartsWith("*") && !startsWithWordPattern;

            bool endsWithAsterisk = text.TrimEnd().EndsWith("*");

            if (startsWithAsterisk && endsWithAsterisk)
                return TextMarkerType.BothEnds;
            else if (startsWithAsterisk)
                return TextMarkerType.StartOnly;
            else if (endsWithAsterisk)
                return TextMarkerType.EndOnly;
            else
                return TextMarkerType.None;
        }


        public void Announce()
        {
            IsAnnounced=true;
        }

    }
}

