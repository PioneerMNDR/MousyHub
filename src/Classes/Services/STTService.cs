using Microsoft.JSInterop;
using System.Diagnostics;
using static MousyHub.Models.ChatState;

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
        // Таймер для отслеживания тишины // Timer for tracking silence
        private System.Timers.Timer? _silenceTimer;
        private const int SILENCE_TIMEOUT = 1500;
        private string _lastRecognizedText = string.Empty;

        // Событие для всех случаев завершения распознавания (тишина или ручная остановка) // Event for all cases of recognition completion (silence or manual stop)
        public event EventHandler<string>? RecognitionCompleted;
        public event TaskDelegate? ErrorRecognition;

        public string Language { get { return settings.User.TranslatorOptions.SelectLanguage.Value; } private set { } }

        public bool isRecord { get; private set; } = false;
        public bool isProcessing { get; private set; } = false;

        // Добавляем флаг для режима накопления текста // Adding a flag for text accumulation mode
        public bool AccumulateText { get; set; } = true;

        // Добавлен опциональный параметр для включения таймера тишины // Added an optional parameter for enabling the silence timer
        public async Task StartRecognition(bool useSilenceTimer = false, bool ResetAccumulatedText = true)
        {
            if (!isRecord)
            {
                isRecord = true;
                _recognitionSubscription?.Dispose();
                _recognitionCompletionSource = new TaskCompletionSource<string>();
                if(ResetAccumulatedText)
                _lastRecognizedText = string.Empty; // Сбрасываем накопленный текст при старте // Reset accumulated text at start
                Debug.WriteLine($"Starting recognition with silence timer: {useSilenceTimer}");
                // Инициализируем таймер только если он включен // Initialize timer only if it's enabled
                if (useSilenceTimer)
                {
                    InitializeSilenceTimer();
                }

                _recognitionSubscription = await speechRecognition.RecognizeSpeechAsync(
                    Language,
                    OnRecognized,
                    OnError,
                    OnStarted,
                    OnEnded
                );
            }
        }

        private void InitializeSilenceTimer()
        {
            // Останавливаем предыдущий таймер, если он существует // Stop the previous timer if it exists
            _silenceTimer?.Stop();
            _silenceTimer?.Dispose();

            // Создаем новый таймер // Create a new timer
            _silenceTimer = new System.Timers.Timer(SILENCE_TIMEOUT);
            _silenceTimer.AutoReset = false; // Одноразовый таймер // One-time timer
          
            _silenceTimer.Elapsed += async (sender, e) =>
            {
             
                if (isRecord)
                {
                   
                    await HandleSilenceDetected();
                }
                else
                {
                    Debug.WriteLine("isRecord is false, ignoring silence");
                }
            };

            // Запускаем таймер // Start the timer
            _silenceTimer.Start();
        }

        private async Task HandleSilenceDetected()
        {
          
            // Выполняем на UI потоке // Execute on the UI thread
            await Task.Run(async () =>
            {
                if (isRecord && !string.IsNullOrEmpty(_lastRecognizedText))
                {
                    isRecord = false;

                    // Останавливаем распознавание // Stop the recognition
                    await speechRecognition.CancelSpeechRecognitionAsync(false);

                    // Короткая пауза для завершения всех процессов // A short pause to complete all processes
                    await Task.Delay(100);


                    RecognitionCompleted?.Invoke(this, _lastRecognizedText);

                    // Устанавливаем результат распознавания // Set the recognition result
                    _recognitionCompletionSource?.TrySetResult(_lastRecognizedText);

                    Debug.WriteLine("Silence detected, recognition stopped automatically.");
                }
            });
        }

        public async Task StopRecognition(bool WithoutResult=false)
        {
            if (!isRecord && WithoutResult==false)
            {
                // Если запись уже остановлена, просто генерируем событие с текущим результатом // If recording is already stopped, just generate an event with the current result
                RecognitionCompleted?.Invoke(this, _lastRecognizedText);
                return;
            }

            try
            {
                isProcessing = true;

                // Останавливаем таймер тишины // Stop the silence timer
                _silenceTimer?.Stop();

                // Отменяем распознавание // Cancel recognition
                await speechRecognition.CancelSpeechRecognitionAsync(false);

                // Задержка для завершения всех процессов распознавания // Delay for completing all recognition processes
                await Task.Delay(200);

                // После задержки устанавливаем флаг остановки записи // After the delay, set the recording stop flag
                isRecord = false;
                if (WithoutResult==false)
                {
                    // Генерируем событие с текущим результатом // Generate an event with the current result
                    RecognitionCompleted?.Invoke(this, _lastRecognizedText);
                }
        
            }
            finally
            {
                isProcessing = false;
                isRecord = false;
                _silenceTimer?.Dispose();
                _silenceTimer = null;
            }
        }

        private async Task OnRecognized(string recognizedText)
        {
            if (AccumulateText)
            {
                // Накапливаем текст // Accumulate text
                if (string.IsNullOrEmpty(_lastRecognizedText))
                {
                    _lastRecognizedText = recognizedText;
                }
                else
                {
                    // Добавляем пробел между фрагментами, если его нет // Add a space between fragments if there isn't one
                    string separator = recognizedText.StartsWith(" ") || _lastRecognizedText.EndsWith(" ") ? "" : " ";
                    _lastRecognizedText += separator + recognizedText;
                }
            }
            else
            {
                // Просто заменяем текст (старое поведение) // Just replace the text (old behavior)
                _lastRecognizedText = recognizedText;
            }
     
            // Сбрасываем таймер тишины если он используется // Reset the silence timer if it's being used
            if (_silenceTimer != null)
            {
                ResetSilenceTimer();
            }

            Debug.WriteLine(recognizedText);
        }

        private void ResetSilenceTimer()
        {
           
            _silenceTimer.Stop();
            _silenceTimer.Start(); 
        }

        private async Task OnError(SpeechRecognitionErrorEvent errorEvent)
        {
            Console.WriteLine($"STT Error: {errorEvent.Error}");
            if (_lastRecognizedText.Length > 0)
            {
                RecognitionCompleted?.Invoke(this, _lastRecognizedText);

             
            }
            isRecord = false;      
            _lastRecognizedText = string.Empty;
            _silenceTimer?.Stop();
            _silenceTimer?.Dispose();
            _silenceTimer = null;
            _recognitionSubscription?.Dispose();
            _recognitionSubscription = null;
            //A very strange error was displayed in Yandex browser
            if (errorEvent.Error == "network") return;
            ErrorRecognition?.Invoke();
        }

        private async Task OnStarted()
        {
            //Console.WriteLine("Speech recognition started.");        
        }

        private async Task OnEnded()
        {

            // Остановка таймера при завершении распознавания // Stop the timer when recognition ends
            _silenceTimer?.Stop();
        }

        // Метод для освобождения ресурсов // Method for releasing resources
        public void Dispose()
        {
            _recognitionSubscription?.Dispose();
            _silenceTimer?.Dispose();
            _silenceTimer = null;
        }
    }
}
