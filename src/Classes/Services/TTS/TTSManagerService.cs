using MousyHub.Models.Services;
using System.Diagnostics;
using System.Text;

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

        private StringBuilder buffer = new StringBuilder();
        private readonly object _lock = new object();
        private HashSet<string> processedSentences = new HashSet<string>();
        private CancellationTokenSource _processingCts = new CancellationTokenSource();
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

                lock (_lock)
                {
                    // Очищаем буфер
                    buffer.Clear();
                }
                // Очищаем очередь аудио
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

                List<(string sentence, bool isThirdPerson)> sentencesToProcess;

                lock (_lock)
                {
                    token.ThrowIfCancellationRequested();

                    // Добавляем новый текст в буфер
                    buffer.Append(newText);
                    string fullText = buffer.ToString();

                    // Разбираем текст из буфера на предложения
                    var result = ParseText(fullText, token, true);
                    sentencesToProcess = result.sentencesToProcess;

                    // Обновляем буфер, если остались незавершенные предложения
                    if (result.currentPos < fullText.Length)
                    {
                        buffer = new StringBuilder(fullText.Substring(result.currentPos));
                    }
                    else
                    {
                        buffer.Clear();
                    }
                }

                // Озвучиваем найденные предложения
                await SpeakSentences(sentencesToProcess, char_voice_name, token);
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

        public async Task ProcessFullText(string fullText, string char_voice_name)
        {
            if (_settingsService.User.TTSOptions.Enabled == false)
                return;

            await InterruptProcessingAsync();
            try
            {
                CancellationToken token = _processingCts.Token;

                // Разбираем полный текст на предложения
                var result = ParseText(fullText, token, false);

                // Озвучиваем найденные предложения
                await SpeakSentences(result.sentencesToProcess, char_voice_name, token);

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

        private (List<(string sentence, bool isThirdPerson)> sentencesToProcess, int currentPos) ParseText(
            string text, CancellationToken token, bool isStreaming)
        {
            List<(string sentence, bool isThirdPerson)> sentencesToProcess = new List<(string, bool)>();
            HashSet<string> localProcessedSentences = isStreaming ? processedSentences : new HashSet<string>();
            int currentPos = 0;

            while (currentPos < text.Length)
            {
                // Периодическая проверка отмены
                if (currentPos % 100 == 0) token.ThrowIfCancellationRequested();

                // Если текущий символ - звездочка, обрабатываем блок от третьего лица
                if (text[currentPos] == '*')
                {
                    // Ищем парную звездочку
                    int endPos = text.IndexOf('*', currentPos + 1);

                    if (endPos != -1)
                    {
                        // Нашли законченный блок от третьего лица
                        string thirdPersonText = text.Substring(currentPos, endPos - currentPos + 1);

                        if (!localProcessedSentences.Contains(thirdPersonText))
                        {
                            sentencesToProcess.Add((thirdPersonText, true));
                            localProcessedSentences.Add(thirdPersonText);
                        }

                        currentPos = endPos + 1;

                        // Пропускаем пробелы после блока от третьего лица
                        while (currentPos < text.Length && char.IsWhiteSpace(text[currentPos]))
                        {
                            currentPos++;
                        }
                    }
                    else
                    {
                        // Незаконченный блок от третьего лица
                        if (!isStreaming)
                        {
                            // Для полного текста - обрабатываем до конца текста
                            string thirdPersonText = text.Substring(currentPos);

                            // Добавим закрывающую звездочку, если её нет
                            if (!thirdPersonText.EndsWith("*"))
                            {
                                thirdPersonText += "*";
                            }

                            if (!localProcessedSentences.Contains(thirdPersonText))
                            {
                                sentencesToProcess.Add((thirdPersonText, true));
                                localProcessedSentences.Add(thirdPersonText);
                            }

                            currentPos = text.Length; // Переходим к концу текста
                        }
                        else
                        {
                            // Для стримингового текста - просто прерываем обработку
                            break;
                        }
                    }
                }
                // Иначе обрабатываем прямую речь
                else
                {
                    // Ищем начало следующего блока от третьего лица
                    int nextAsterisk = text.IndexOf('*', currentPos);
                    int sentenceEnd = -1;
                    int capitalLetterPos = -1;

                    // Ищем конец предложения до следующей звездочки или до конца текста
                    for (int i = currentPos; i < (nextAsterisk != -1 ? nextAsterisk : text.Length); i++)
                    {
                        // Проверяем наличие знаков препинания, обозначающих конец предложения
                        if ((text[i] == '.' || text[i] == '!' || text[i] == '?') &&
                            (i + 1 == text.Length || i + 1 == nextAsterisk ||
                             char.IsWhiteSpace(text[i + 1]) ||
                             (i + 1 < text.Length && char.IsUpper(text[i + 1]))))
                        {
                            sentenceEnd = i;
                            break;
                        }

                        // Если включена опция использования заглавной буквы как разделителя
                        if (_settingsService.User.TTSOptions.UseCapitalLetterAsBreak && capitalLetterPos == -1 && i > currentPos + 1)
                        {
                            // Проверяем, что текущий символ - заглавная буква
                            // и предыдущий символ - пробел
                            // и перед пробелом не стоит знак препинания
                            if (char.IsUpper(text[i]) && i > 0 && text[i - 1] == ' ' &&
                                (i <= currentPos + 1 || (text[i - 2] != '.' && text[i - 2] != '!' && text[i - 2] != '?')))
                            {
                                capitalLetterPos = i - 1; // Позиция перед заглавной буквой
                            }
                        }
                    }

                    // Если нашли конец предложения по знаку препинания
                    if (sentenceEnd != -1)
                    {
                        string sentence = text.Substring(currentPos, sentenceEnd - currentPos + 1).Trim();

                        if (!string.IsNullOrEmpty(sentence) && !localProcessedSentences.Contains(sentence))
                        {
                            sentencesToProcess.Add((sentence, false));
                            localProcessedSentences.Add(sentence);
                        }

                        currentPos = sentenceEnd + 1;

                        // Пропускаем пробелы
                        while (currentPos < text.Length && char.IsWhiteSpace(text[currentPos]))
                        {
                            currentPos++;
                        }
                    }
                    // Если не нашли конец предложения по знаку препинания, но нашли заглавную букву
                    else if (_settingsService.User.TTSOptions.UseCapitalLetterAsBreak && capitalLetterPos != -1)
                    {
                        string sentence = text.Substring(currentPos, capitalLetterPos - currentPos + 1).Trim();

                        if (!string.IsNullOrEmpty(sentence) && !localProcessedSentences.Contains(sentence))
                        {
                            // Добавляем точку в конец предложения, если её нет
                            if (!sentence.EndsWith(".") && !sentence.EndsWith("!") && !sentence.EndsWith("?"))
                            {
                                sentence += ".";
                            }

                            sentencesToProcess.Add((sentence, false));
                            localProcessedSentences.Add(sentence);
                        }

                        currentPos = capitalLetterPos + 1;
                    }
                    // Если не нашли конец предложения, но есть следующий блок от третьего лица
                    else if (nextAsterisk != -1)
                    {
                        // Если есть текст перед звездочкой, обрабатываем его как отдельное предложение
                        if (nextAsterisk > currentPos)
                        {
                            string sentence = text.Substring(currentPos, nextAsterisk - currentPos).Trim();

                            if (!string.IsNullOrEmpty(sentence) && !localProcessedSentences.Contains(sentence))
                            {
                                // Добавляем точку в конец предложения, если её нет
                                if (!sentence.EndsWith(".") && !sentence.EndsWith("!") && !sentence.EndsWith("?"))
                                {
                                    sentence += ".";
                                }

                                sentencesToProcess.Add((sentence, false));
                                localProcessedSentences.Add(sentence);
                            }
                        }

                        // Переходим к обработке блока от третьего лица
                        currentPos = nextAsterisk;
                    }
                    // Если не нашли ни конец предложения, ни следующий блок от третьего лица
                    else
                    {
                        if (!isStreaming)
                        {
                            // Для полного текста - обрабатываем до конца текста
                            string sentence = text.Substring(currentPos).Trim();

                            if (!string.IsNullOrEmpty(sentence) && !localProcessedSentences.Contains(sentence))
                            {
                                // Добавляем точку в конец предложения, если её нет
                                if (!sentence.EndsWith(".") && !sentence.EndsWith("!") && !sentence.EndsWith("?"))
                                {
                                    sentence += ".";
                                }

                                sentencesToProcess.Add((sentence, false));
                                localProcessedSentences.Add(sentence);
                            }

                            currentPos = text.Length; // Переходим к концу текста
                        }
                        else
                        {
                            // Для стримингового текста - просто прерываем обработку
                            break;
                        }
                    }
                }
            }

            return (sentencesToProcess, currentPos);
        }

        private async Task SpeakSentences(List<(string sentence, bool isThirdPerson)> sentences,
            string char_voice_name, CancellationToken token)
        {
            foreach (var (sentence, isThirdPerson) in sentences)
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    byte[] wavData;
                    if (isThirdPerson && _settingsService.User.TTSOptions.SplitVoice)
                    {
                        wavData = await _kokoroService.GetSpeak(sentence, _settingsService.User.TTSOptions.NarratorKokoroVoice);
                        Debug.WriteLine("Third person: " + sentence);
                    }
                    else
                    {
                        wavData = await _kokoroService.GetSpeak(sentence, char_voice_name);
                        Debug.WriteLine("First person: " + sentence);
                    }

                    token.ThrowIfCancellationRequested();
                    await _audioService.EnqueueAudioAsync(wavData);
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("Завершение предложения было отменено");
                }

            }
        }

        public async Task EndSentenceThread(string char_voice_name)
        {
            if (_settingsService.User.TTSOptions.Enabled == false)
                return;

            try
            {
                CancellationToken token = _processingCts.Token;
                List<(string sentence, bool isThirdPerson)> sentencesToProcess = new List<(string, bool)>();

                lock (_lock)
                {
                    token.ThrowIfCancellationRequested();

                    string remainingText = buffer.ToString();
                    if (!string.IsNullOrEmpty(remainingText))
                    {
                        // Определяем, является ли оставшийся текст текстом от третьего лица
                        bool isThirdPerson = remainingText.StartsWith("*");

                        if (isThirdPerson)
                        {
                            // Если текст не заканчивается звездочкой, добавляем её
                            if (!remainingText.EndsWith("*"))
                            {
                                remainingText += "*";
                            }
                        }
                        else
                        {
                            // Если текст не заканчивается на знак препинания, добавляем точку
                            if (!remainingText.EndsWith(".") && !remainingText.EndsWith("!") && !remainingText.EndsWith("?"))
                            {
                                remainingText += ".";
                            }
                        }

                        if (!processedSentences.Contains(remainingText))
                        {
                            sentencesToProcess.Add((remainingText, isThirdPerson));
                            processedSentences.Add(remainingText);
                        }

                        buffer.Clear();
                    }
                }

                // Используем общий метод для озвучивания
                await SpeakSentences(sentencesToProcess, char_voice_name, token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Завершение предложения было отменено");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при завершении предложения: {ex.Message}");
            }
        }
    }

}
