using Microsoft.JSInterop;

namespace MousyHub.Models.Services
{
    public class STTService
    {
        private readonly ISpeechRecognitionService speechRecognition;
        private readonly SettingsService settings;
        public STTService(ISpeechRecognitionService speechRecognition, SettingsService settings)
        {
            this.speechRecognition = speechRecognition;
            this.settings = settings;
        }
        private IDisposable? _recognitionSubscription;
        private TaskCompletionSource<string>? _recognitionCompletionSource;
        public string Language { get { return settings.User.TranslatorOptions.SelectLanguage.Value; } private set { } }

        public bool isRecord { get; private set; } = false;
        public async Task StartRecognition()
        {
            if (!isRecord)
            {
                isRecord = true;
                _recognitionSubscription?.Dispose();
                _recognitionCompletionSource = new TaskCompletionSource<string>();
                _recognitionSubscription = await speechRecognition.RecognizeSpeechAsync(
                    Language,
                    OnRecognized,
                    OnError,
                    OnStarted,
                    OnEnded
                );
            }
        }
        public async Task<string> StopRecognition()
        {

                string recognizedText = await _recognitionCompletionSource.Task;
                await speechRecognition.CancelSpeechRecognitionAsync(false);
            isRecord = false;
            return recognizedText;
        }
        private async Task OnRecognized(string recognizedText)
        {
            _recognitionCompletionSource?.TrySetResult(recognizedText);
        }

        private async Task OnError(SpeechRecognitionErrorEvent errorEvent)
        {
            // Обработка ошибки
            Console.WriteLine($"Error: {errorEvent.Error}");
        }

        private async Task OnStarted()
        {
            //Console.WriteLine("Speech recognition started.");        
        }

        private async Task OnEnded()
        {
            //Console.WriteLine("Speech recognition ended.");
            isRecord = false;
        }


    }
}
