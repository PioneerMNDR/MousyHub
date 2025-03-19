namespace MousyHub.Classes.Services.TTS
{
    using Microsoft.JSInterop;
    using MousyHub.Models;
    using MousyHub.Models.Services;
    using NAudio.Wave;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using static MousyHub.Models.ChatState;

    public class AudioService : IAsyncDisposable
    {
        public delegate Task PersonDelegate(Person? person);
        public event PersonDelegate QueueEmptyEvent;
        public event PersonDelegate StartAudioEvent;
        private readonly IJSRuntime _jsRuntime;
        private readonly Queue<AudioQueueItem> _audioQueue = new Queue<AudioQueueItem>();
        private bool _isPlaying = false;
        private CancellationTokenSource _currentPlaybackCts;
        private bool _isPaused = false;
        private readonly SemaphoreSlim _queueSemaphore = new SemaphoreSlim(1, 1);
        private bool _isProcessingQueue = false;
        private readonly object _processingLock = new object();

        private Timer _silenceDetectionTimer;
        private readonly object _timerLock = new object();
        private const int SILENCE_THRESHOLD_MS = 500; // Порог тишины (настройте по необходимости)

        Person? LastPerson = null;
        SettingsService _settings;
        public AudioService(IJSRuntime jsRuntime, SettingsService settings)
        {
            _jsRuntime = jsRuntime;
            _settings = settings;
            InitializeTimer();
        }

        public ValueTask DisposeAsync()
        {
            _queueSemaphore.Dispose();
            _currentPlaybackCts?.Dispose();
            _silenceDetectionTimer?.Dispose();
            return ValueTask.CompletedTask;
        }
        private void InitializeTimer()
        {
            _silenceDetectionTimer = new Timer(_ =>
            {
                QueueEmptyEvent?.Invoke(LastPerson);
            }, null, Timeout.Infinite, Timeout.Infinite);
        }

        public async Task EnqueueAudioAsync(byte[] rawAudioData, string mimeType = "audio/wav", Person? person = null)
        {
            if (rawAudioData == null || rawAudioData.Length == 0)
            {
                Console.WriteLine("Ошибка: аудио данные пусты или null"); // Error: audio data is empty or null
                return;
            }

            // Преобразуем raw PCM данные в полноценный WAV // Convert raw PCM data to full WAV
            byte[] wavData = ConvertToWavWithHeader(rawAudioData);
            LastPerson = person;
            await _queueSemaphore.WaitAsync();
            try
            {
                lock (_timerLock)
                {
                    _silenceDetectionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                var queueItem = new AudioQueueItem(wavData, "audio/wav");
                _audioQueue.Enqueue(queueItem);

                if (!_isPlaying && !_isPaused)
                {
                    _isPlaying = true;
                    _ = ProcessQueueAsync(person);
                }
            }
            finally
            {
                _queueSemaphore.Release();
            }
        }

        private async Task ProcessQueueAsync(Person? person)
        {
            // Защита от одновременного запуска нескольких экземпляров // Protection against simultaneous launch of multiple instances
            lock (_processingLock)
            {
                if (_isProcessingQueue) return;
                _isProcessingQueue = true;
            }

            try
            {
                while (true)
                {
                    AudioQueueItem currentItem = null;
                    bool shouldReleaseSemaphore = false;

                    try
                    {
                        await _queueSemaphore.WaitAsync();
                        shouldReleaseSemaphore = true;

                        if (_audioQueue.Count == 0 || _isPaused)
                        {
                            _isPlaying = _audioQueue.Count > 0;
                            _queueSemaphore.Release(); // Освобождаем семафор перед выходом // Release the semaphore before exiting
                            shouldReleaseSemaphore = false;
                            break;
                        }

                        currentItem = _audioQueue.Peek();
                        _currentPlaybackCts = new CancellationTokenSource();

                        _queueSemaphore.Release();
                        shouldReleaseSemaphore = false;
                    }
                    catch
                    {
                        if (shouldReleaseSemaphore)
                        {
                            _queueSemaphore.Release();
                        }
                        throw;
                    }

                    if (currentItem != null)
                    {
                        try
                        {
                            await PlayAudioInternalAsync(
                                currentItem.AudioData,
                                currentItem.MimeType,
                                _settings.User.TTSOptions.PlaybackSpeed,
                                _settings.User.TTSOptions.VolumeValue,
                                _currentPlaybackCts.Token, person);

                            shouldReleaseSemaphore = false;
                            await _queueSemaphore.WaitAsync();
                            shouldReleaseSemaphore = true;

                            try
                            {
                                if (_audioQueue.Count > 0 && _audioQueue.Peek() == currentItem)
                                {
                                    _audioQueue.Dequeue();
                                    if (_audioQueue.Count == 0)
                                    {
                                        lock (_timerLock)
                                        {
                                            _silenceDetectionTimer.Change(SILENCE_THRESHOLD_MS, Timeout.Infinite);
                                        }
                                    }

                                }
                            }
                            finally
                            {
                                if (shouldReleaseSemaphore)
                                {
                                    _queueSemaphore.Release();
                                    shouldReleaseSemaphore = false;
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Воспроизведение было отменено // Playback was canceled
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка воспроизведения аудио: {ex.Message}"); // Audio playback error: {ex.Message}
                        }
                    }
                }
            }
            finally
            {
                lock (_processingLock)
                {
                    _isProcessingQueue = false;                   
                }
            }
        }

        // Метод для добавления WAV-заголовка к аудиоданным // Method for adding WAV header to audio data
        private byte[] ConvertToWavWithHeader(byte[] rawAudioData)
        {
            using var memoryStream = new MemoryStream();
            var waveFormat = new WaveFormat(24000, 16, 1);

            using (var writer = new WaveFileWriter(memoryStream, waveFormat))
            {
                writer.Write(rawAudioData, 0, rawAudioData.Length);
            }

            return memoryStream.ToArray();
        }


        private async Task PlayAudioInternalAsync(byte[] audioData, string mimeType, float playbackRate, float volume, CancellationToken cancellationToken, Person? person)
        {
            try
            {
                // Проверим формат WAV, MP3 или другой // Check the format: WAV, MP3 or other
                if (mimeType.Contains("mp3"))
                {
                    mimeType = "audio/mpeg";
                }
                else if (mimeType.Contains("wav") || mimeType.Contains("wave"))
                {
                    mimeType = "audio/wav";
                }

                string base64String = Convert.ToBase64String(audioData);
                Debug.WriteLine($"Аудио размер: {audioData.Length} байт, MIME: {mimeType}, Скорость воспроизведения: {playbackRate}, Громкость: {volume}"); // Audio size: {audioData.Length} bytes, MIME: {mimeType}, Playback speed: {playbackRate}, Volume: {volume}
                StartAudioEvent?.Invoke(person);
                // Передаем playbackRate и volume в JavaScript // Pass playbackRate and volume to JavaScript
                var audioId = await _jsRuntime.InvokeAsync<int>("playAudio", cancellationToken, base64String, mimeType, playbackRate, volume, _settings.User.VoiceMode);

                // Ожидаем завершения воспроизведения или отмены // Wait for playback completion or cancellation
                var completionSource = new TaskCompletionSource<bool>();

                // Обработчик события завершения воспроизведения // Handler for playback completion event
                DotNetObjectReference<AudioCompletionCallback> callbackRef = null;

                try
                {
                    var callback = new AudioCompletionCallback(() => completionSource.TrySetResult(true));
                    callbackRef = DotNetObjectReference.Create(callback);

                    await _jsRuntime.InvokeVoidAsync("registerAudioEndedCallback",
                        cancellationToken, audioId, callbackRef);

                    // Регистрируем отмену // Register cancellation
                    using var registration = cancellationToken.Register(() =>
                    {
                        completionSource.TrySetResult(false);
                        _jsRuntime.InvokeVoidAsync("stopAudio", audioId);
                    });
                    bool completed = await completionSource.Task;
                    if (!completed)
                    {
                        Debug.WriteLine("Audio playback was canceled"); 
                    }
                }
                finally
                {
                    callbackRef?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PlayAudioInternalAsync: {ex.Message}");
            }
        }
        public async Task PauseAsync()
        {
            if (!_isPaused && _isPlaying)
            {
                _isPaused = true;
                await _jsRuntime.InvokeVoidAsync("pauseCurrentAudio");
            }
        }

        public async Task ResumeAsync()
        {
            if (_isPaused)
            {
                _isPaused = false;
                await _jsRuntime.InvokeVoidAsync("resumeCurrentAudio");

                // Возобновляем обработку очереди // Resume queue processing
                if (_audioQueue.Count > 0)
                {
                    _isPlaying = true;
                    _ = ProcessQueueAsync(LastPerson);
                }
            }
        }

        public async Task CancelCurrentAsync()
        {
            _currentPlaybackCts?.Cancel();
            await _jsRuntime.InvokeVoidAsync("stopCurrentAudio");
        }

        public async Task ClearQueueAsync()
        {
            await _queueSemaphore.WaitAsync();
            try
            {
                bool Elements_Existed = false;
                // Оставляем текущий элемент, удаляем остальные // Keep the current item, remove the rest
                if (_audioQueue.Count > 0)
                {
                    Elements_Existed = true;
                     var currentItem = _audioQueue.Dequeue();
                    _audioQueue.Clear();
                    if (_isPlaying && !_isPaused)
                    {
                        _audioQueue.Enqueue(currentItem);
                    }
                }
                if (_audioQueue.Count == 0 && Elements_Existed)
                {
                    QueueEmptyEvent?.Invoke(null);
                }
            }
            finally
            {
                _queueSemaphore.Release();
            }
        }

        public async Task StopAllAsync()
        {
            await _queueSemaphore.WaitAsync();
            try
            {
                _audioQueue.Clear();
                _isPaused = false;
                _isPlaying = false;
                _currentPlaybackCts?.Cancel();
                await _jsRuntime.InvokeVoidAsync("stopAllAudio");
            }
            finally
            {
                _queueSemaphore.Release();
            }
        }
    }


    public class AudioQueueItem
    {
        public byte[] AudioData { get; }
        public string MimeType { get; }

        public AudioQueueItem(byte[] audioData, string mimeType)
        {
            AudioData = audioData;
            MimeType = mimeType;
        }
    }

    public class AudioCompletionCallback
    {
        private readonly Action _callback;

        public AudioCompletionCallback(Action callback)
        {
            _callback = callback;
        }

        [JSInvokable]
        public void OnAudioEnded()
        {
            _callback();
        }
    }
}