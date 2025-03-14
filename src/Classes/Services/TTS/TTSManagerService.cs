using MousyHub.Classes.Misc;
using MousyHub.Models.Services;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

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
        private List<KokoroSentence> Sentences = new List<KokoroSentence>();
        private string Buffer { get; set; }
        bool NarratorSequence = false;
        public async Task InterruptProcessingAsync()
        {
            if (_settingsService.User.TTSOptions.Enabled)
            {
                // Отменяем текущую обработку
                if (!_processingCts.IsCancellationRequested)
                {
                    _processingCts.Cancel();

                    // Создаем новый источник токена отмены для будущих операций
                    _processingCts = new CancellationTokenSource();
                }

                // Очищаем строковый буфер вместо коллекции
                Buffer = string.Empty;

                // Сбрасываем состояние нарратора
                NarratorSequence = false;

                // Очистка очереди аудио
                await _audioService.StopAllAsync();
                await _audioService.ClearQueueAsync();

                Debug.WriteLine("Обработка текста прервана");
            }
        }


        public async Task ProcessStreamingText(string newText, string char_voice_name)
        {
            if (_settingsService.User.TTSOptions.Enabled == false)
                return;

            try
            {
                CancellationToken token = _processingCts.Token;
                Buffer += newText;

                // Parse the text and get only new sentences
                var newSentences = await ParseTextAndGetNewSentences(Buffer, token);

                // Only speak these new sentences
                await SpeakSentencesList(newSentences, char_voice_name, token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Обработка текста была отменена");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке текста: {ex.Message}");
            }
        }

        private async Task<List<KokoroSentence>> ParseTextAndGetNewSentences(string text, CancellationToken token)
        {
            text = text.Replace("\r\n", " ").Replace("\n", " ");
            string[] parts = text.SplitWithDefaultSeparators();

            var newSentences = new List<KokoroSentence>();
            foreach (string part in parts)
            {
                // Use a more robust way to check if this sentence already exists
                if (!Sentences.Any(x => x.Text.Equals(part, StringComparison.Ordinal)))
                {
                    var newSent = new KokoroSentence(part, false);
                    if (newSent.textMarkerType is KokoroSentence.TextMarkerType.StartOnly)
                        NarratorSequence = true;
                    if (NarratorSequence && newSent.textMarkerType is KokoroSentence.TextMarkerType.None)
                        newSent.IsNarrator = true;
                    if (NarratorSequence && newSent.textMarkerType is KokoroSentence.TextMarkerType.EndOnly)
                        NarratorSequence = false;

                    Sentences.Add(newSent);
                    newSentences.Add(newSent);
                }
            }

            return newSentences;
        }
        private async Task SpeakSentencesList(List<KokoroSentence> sentencesToSpeak, string char_voice_name, CancellationToken token)
        {
            foreach (var sentence in sentencesToSpeak)
            {
                try
                {
                    token.ThrowIfCancellationRequested();

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

                    token.ThrowIfCancellationRequested();
                    sentence.Announce();
                    await _audioService.EnqueueAudioAsync(wavData);
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("Завершение предложения было отменено");
                }
            }
        }
        public async Task ProcessFullText(string fullText, string char_voice_name)
        {
            if (_settingsService.User.TTSOptions.Enabled == false)
                return;

            await InterruptProcessingAsync();
            try
            {
                CancellationToken token = _processingCts.Token;
                NarratorSequence = false;
                Sentences.Clear();

                // Parse the full text and get all sentences (they're all new since we cleared the collection)
                var allSentences = await ParseTextAndGetNewSentences(fullText, token);

                // Speak all sentences using our new method
                await SpeakSentencesList(allSentences, char_voice_name, token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Обработка текста была отменена");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке текста: {ex.Message}");
            }
        }

        public async Task EndSentenceThread(string char_voice_name)
        {
            if (_settingsService.User.TTSOptions.Enabled == false)
                return;

            try
            {
                // Parse any remaining text in buffer that might not have been processed yet
                var finalSentences = await ParseTextAndGetNewSentences(Buffer, _processingCts.Token);

                // Only process these new sentences that were just parsed
                if (finalSentences.Any())
                {
                    await SpeakSentencesList(finalSentences, char_voice_name, _processingCts.Token);
                }

                // Clean up everything
                NarratorSequence = false;
                Sentences.Clear();
                Buffer = string.Empty;

                Console.WriteLine("Поток предложений завершен");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при завершении потока: {ex.Message}");
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
            None,           // Нет звездочек
            StartOnly,      // Звездочка только в начале
            EndOnly,        // Звездочка только в конце
            BothEnds        // Звездочки в начале и в конце
        }
        public static TextMarkerType DetectMarkerType(string text)
        {
            if (string.IsNullOrEmpty(text))
                return TextMarkerType.None;

            bool startsWithAsterisk = text.TrimStart().StartsWith("*");
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

