using Microsoft.Extensions.Options;
using MousyHub.Models;
using MousyHub.Models.Services;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using MousyHub.Models.Services.URLHandle;
using DocumentFormat.OpenXml.ExtendedProperties;



namespace MousyHub.Classes.Services.TelegramExtra
{
    public class TelegramService : IAsyncDisposable
    {
        private readonly AdvancedQueryService _queryService;
        private readonly ProviderService _providerService;
        private ChatState? _chatState;
        private SettingsService? settingsService;
        private TranslatorService? translatorService;
        private URLImporterService ImporterService;
        private readonly ILogger<TelegramService> _logger;

        private ITelegramBotClient? _botClient;
        private CancellationTokenSource? _cts;

        public bool IsReceiving { get; private set; } = false;
        private bool IsBusy { get; set; } = false;
        private readonly Dictionary<long, List<int>> _carouselMessages = new();
        public TelegramService(
            AdvancedQueryService queryService,
            ProviderService providerService,
            ILogger<TelegramService> logger)
        {
            _queryService = queryService;
            _providerService = providerService;
            _logger = logger;
        }

        public async Task StartReceivingAsync(SettingsService settings, ChatState chatState, TranslatorService translator, URLImporterService importerService, CancellationToken cancellationToken = default)
        {
            if (IsReceiving)
            {
                _logger.LogInformation("Telegram bot is already receiving updates.");
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.User.TelegramOptions.BotToken))
            {
                _logger.LogError("Cannot start Telegram bot: BotToken is not configured.");
                return;
            }

            _botClient = new TelegramBotClient(settings.User.TelegramOptions.BotToken);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                var me = await _botClient.GetMe(_cts.Token); // Use GetMeAsync to get bot info
                _chatState = chatState;
                settingsService = settings;
                translatorService = translator;
                ImporterService = importerService;
                _logger.LogInformation($"Bot connected: {me.Username} (ID: {me.Id})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Telegram API. Check your Bot Token and network connection.");
                _botClient = null;
                _cts?.Dispose();
                _cts = null;
                return;
            }

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // Receive all update types
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: _cts.Token
            );

            // Setting bot name and commands can be asynchronous
            //// You may wrap it in Task.Run or use await directly if sequential execution is critical
            //await _botClient.SetMyName("MousyHubBot", cancellationToken: _cts.Token);

            var commands = new[]
            {
                   new BotCommand
                {
                    Command = "start",
                    Description = "👋 Check the bot's readiness and server configuration"
                },
                new BotCommand
                {
                    Command = "character",
                    Description = "🧙‍♂️ Choose a character to chat with in your story"
                },
                new BotCommand
                {
                    Command = "newchat",
                    Description = "🆕 Start a fresh conversation from scratch"
                },
                new BotCommand
                {
                    Command = "addnew",
                    Description = "➕ Add a new character using a shareable link (chub.ai)"
                }
             };

            await _botClient.SetMyCommands(commands, cancellationToken: _cts.Token);
            IsReceiving = true;
            _logger.LogInformation("Telegram bot started receiving updates.");
        }

        public Task StopReceivingAsync()
        {
            if (!IsReceiving || _cts == null)
            {
                _logger.LogInformation("Telegram bot is not currently receiving or already stopped.");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Stopping Telegram bot...");
            _cts.Cancel(); // Это остановит StartReceiving
            IsReceiving = false;
            // _botClient сам остановится при отмене токена
            _logger.LogInformation("Telegram bot stopped.");
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await HandleCallbackQuery(botClient, update.CallbackQuery, cancellationToken);
                return;
            }

            if (update.Type != UpdateType.Message || update.Message?.Text == null)
                return;

            var message = update.Message;
            var chatId = message.Chat.Id;
            var messageText = message.Text;

            _logger.LogInformation($"Received a '{messageText}' message in chat {chatId} from user {message.From?.Username ?? message.From?.FirstName}.");

            if (messageText.StartsWith("/"))
            {
                await HandleCommandAsync(botClient, message, cancellationToken);
            }
            else
            {
                // --- Start of typing animation implementation ---

                using var typingCts = new CancellationTokenSource();
                // Create a linked cancellation token source which will trigger if either the main token or typingCts is cancelled
                var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, typingCts.Token);

                // Start a background task to periodically send the ChatAction.Typing
                Task typingTask = Task.Run(async () =>
                {
                    try
                    {
                        while (!combinedCts.Token.IsCancellationRequested)
                        {
                            await botClient.SendChatAction(
                                chatId: chatId,
                                action: ChatAction.Typing,
                                cancellationToken: combinedCts.Token);
                            // Telegram clears the "typing" status after about 5 seconds,
                            // so we resend it every 4 seconds.
                            await Task.Delay(TimeSpan.FromSeconds(4), combinedCts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("Typing task for chat {ChatId} was cancelled.", chatId);
                    }
                    catch (ApiRequestException apiEx) when (apiEx.Message.Contains("chat not found"))
                    {
                        _logger.LogWarning(apiEx, "Typing task failed for chat {ChatId}: Chat not found. User might have blocked the bot.", chatId);
                        // You can take action here, e.g., mark chat as inactive
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in typing task for chat {ChatId}", chatId);
                    }
                }, combinedCts.Token); // Pass combinedCts.Token to allow early cancellation of the task

                string botAnswer;
                try
                {
                    // Send "typing" once immediately so the user sees the indicator as soon as possible
                    await botClient.SendChatAction(
                        chatId: chatId,
                        action: ChatAction.Typing,
                        cancellationToken: cancellationToken); // Use the main cancellationToken

                    botAnswer = await Generate(messageText); // Your LLM generation

                    typingCts.Cancel(); // Cancel the "typing" task as the answer is ready
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating bot answer for chat {ChatId} with message '{MessageText}'", chatId, messageText);
                    typingCts.Cancel(); // Cancel typing on error as well
                    botAnswer = "Unfortunately, an error occurred while processing your request. Please try again later.";
                }
                finally
                {
                    // Await the typing task to avoid race conditions
                    // and ensure all resources are properly released
                    try
                    {
                        await typingTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected exception if typingTask was cancelled
                    }
                    // Other exceptions (not caught inside the typing task) will be rethrown here
                }

                // Use SendTextMessageAsync to send a text reply
                await botClient.SendMessage(
                    chatId: chatId,
                    text: $"{botAnswer}", // Ensure botAnswer is not null or empty
                    cancellationToken: cancellationToken);

                // --- End of typing animation implementation ---
            }
        }

        private async Task HandleCommandAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
        {
            var command = message.Text?.Split(' ')[0].ToLowerInvariant();
            var chatId = message.Chat.Id;

            // Check for null on _chatState and services before using
            if (_chatState == null || settingsService == null || translatorService == null)
            {
                _logger.LogWarning("Attempted to handle command, but essential services are not initialized for chat {ChatId}.", chatId);
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "The bot is not fully initialized yet. Please wait a moment or try the /start command later.",
                    cancellationToken: cancellationToken);
                return;
            }

            switch (command)
            {
                case "/start":

                    // Получение информации
                    var status = _providerService.Status;
                    var instructName = settingsService.CurrentInstruct?.name ?? "Not set";
                    var configName = settingsService.CurrentGenerationConfig?.ConfigName ?? "Not set";

                    string modelName;
                    if (_providerService.LLModel != null)
                        modelName = await _providerService.LLModel.Model();
                    else
                        modelName = "No connection";

                    // Формируем текст ответа
                    var startMessage =
                        "👋 Hi, I'm ready!\n\n" +
                        "📡 *Connection Information:*\n" +
                        $"• *Provider Status:* `{status}`\n" +
                        $"• *Instruction Preset:* `{instructName}`\n" +
                        $"• *Generation Config:* `{configName}`\n" +
                        $"• *Model Name:* `{modelName}`\n\n" +
                        "💬 Send me a message or use the menu to start.";

                    // Отправка сообщения
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: startMessage,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);

                    break;

                case "/ping":
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Pong!",
                        cancellationToken: cancellationToken);
                    break;

                case "/addnew":
                    var args = message.Text?.Split(' ');
                    if (args.Length < 2)
                    {
                        await botClient.SendMessage(
                            chatId: chatId,
                            text: "❗ Please provide a valid character link.\nExample: /addnew https://example.com/character",
                            cancellationToken: cancellationToken);
                        break;
                    }

                    var characterUrl = args[1];
                    await ImporterService.ImportURL(characterUrl, false);
                    await botClient.SendMessage(
                    chatId: chatId,
           text: $"✅ Character was successfully added!",
           parseMode: ParseMode.Markdown,
           cancellationToken: cancellationToken);
                    break;

                case "/character":
                    await SendCharactersCarousel(botClient, chatId, cancellationToken);
                    break;

                case "/newchat":
                    if (_chatState.ChatHistory != null)
                    {
                        await _chatState.ClearAndNewChatHistory();
                        await botClient.SendMessage(
                            chatId: chatId,
                            text: "🆕 New chat started. Chat history cleared!",
                            parseMode: ParseMode.Markdown,
                            cancellationToken: cancellationToken);

                        foreach (var item in _chatState.ChatHistory.Messages)
                        {
                            await Task.Delay(500); // Optional delay for better UX
                            await botClient.SendMessage(
                                chatId: chatId,
                                text: (item.Owner.IsUser ? $"*👤 {item.Owner.Name}:* " : "") +
                                      (!string.IsNullOrEmpty(item.UserNativeLanguageContent)
                                          ? item.UserNativeLanguageContent
                                          : item.Content),
                                parseMode: ParseMode.Markdown,
                                cancellationToken: cancellationToken);
                        }
                    }
                    break;

                default:
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Unknown command. Try /start, /ping, or /character.",
                        cancellationToken: cancellationToken);
                    break;
            }
        }
        private async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (callbackQuery.Message == null) // Added null check
            {
                _logger.LogWarning("Received CallbackQuery with null Message. CallbackQueryId: {CallbackQueryId}", callbackQuery.Id);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, "An error occurred (no message).", cancellationToken: cancellationToken);
                return;
            }

            var callbackData = callbackQuery.Data;
            var chatId = callbackQuery.Message.Chat.Id;

            // Check for null on _chatState and services before using
            if (_chatState == null || settingsService == null || translatorService == null)
            {
                _logger.LogWarning("Attempted to handle callback query, but essential services are not initialized for chat {ChatId}.", chatId);
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    "The bot is not fully initialized yet. Please try again later.",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return;
            }

            // Answer the callback to remove the "loading" animation (the hourglass)
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken); // Use AnswerCallbackQueryAsync

            if (string.IsNullOrEmpty(callbackData)) // Added null/empty check
            {
                _logger.LogWarning("Received CallbackQuery with empty data. ChatId: {ChatId}, MessageId: {MessageId}", chatId, callbackQuery.Message.MessageId);
                return;
            }

            // Character selection handling
            if (callbackData.StartsWith("char_"))
            {
                var characterId = callbackData.Substring(5);
                var character = _chatState.AllPersons.FirstOrDefault(p => p.Id == characterId);

                if (character != null)
                {
                    // Save selected character
                    await _chatState.CheckChatHistory(character.CharacterCard);
                    // await botClient.SetMyName("[Mousy] " + character.Name, cancellationToken: cancellationToken);

                    // Delete all carousel messages
                    if (_carouselMessages.TryGetValue(chatId, out var messageIds) && messageIds != null)
                    {
                        foreach (var messageId in messageIds)
                        {
                            try
                            {
                                await botClient.DeleteMessage(chatId, messageId, cancellationToken); // Use DeleteMessageAsync
                                await Task.Delay(75, cancellationToken); // Slightly increased delay
                            }
                            catch (ApiRequestException ex) when (ex.Message.Contains("message to delete not found"))
                            {
                                _logger.LogWarning("Message {MessageId} to delete not found in chat {ChatId}. It might have been deleted already.", messageId, chatId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete message {MessageId} in chat {ChatId}", messageId, chatId);
                            }
                        }
                        _carouselMessages.Remove(chatId);
                    }

                    // Send character selection confirmation
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"✅ You selected character: **{character.Name}**\n\n" +
                              "You can now start chatting!",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);

                    await Task.Delay(1000, cancellationToken);

                    if (_chatState.ChatHistory != null)
                    {
                        foreach (var item in _chatState.ChatHistory.Messages)
                        {
                            await Task.Delay(500);
                            await botClient.SendMessage(
                                chatId: chatId,
                                text: (item.Owner.IsUser ? $"*👤 {item.Owner.Name}:* " : "") +
                                      (!string.IsNullOrEmpty(item.UserNativeLanguageContent) ? item.UserNativeLanguageContent : item.Content),
                                parseMode: ParseMode.Markdown,
                                cancellationToken: cancellationToken);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Character with ID '{CharacterId}' not found for callback in chat {ChatId}.", characterId, chatId);
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "😔 Failed to find the selected character. Please try again.",
                        cancellationToken: cancellationToken);
                }
            }
            else if (callbackData == "cancel_char_selection")
            {
                // On cancel, delete the carousel as well
                if (_carouselMessages.TryGetValue(chatId, out var messageIds) && messageIds != null)
                {
                    foreach (var messageId in messageIds)
                    {
                        try
                        {
                            await botClient.DeleteMessage(chatId, messageId, cancellationToken); // Use DeleteMessageAsync
                            await Task.Delay(75, cancellationToken);
                        }
                        catch (ApiRequestException ex) when (ex.Message.Contains("message to delete not found"))
                        {
                            _logger.LogWarning("Message {MessageId} to delete not found (cancel selection) in chat {ChatId}.", messageId, chatId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete message {MessageId} (cancel selection) in chat {ChatId}", messageId, chatId);
                        }
                    }
                    _carouselMessages.Remove(chatId);
                }

                await botClient.SendMessage(
                    chatId: chatId,
                    text: "❌ Character selection canceled",
                    cancellationToken: cancellationToken);
            }
        }
        private async Task SendCharactersCarousel(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            if (_chatState == null)
            {
                _logger.LogWarning("Cannot send characters carousel: _chatState is null for chat {ChatId}.", chatId);
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "😔 Failed to load character list (internal error). Please try again later.",
                    cancellationToken: cancellationToken);
                return;
            }

            var characters = _chatState.AllPersons.Where(p => p != null && !p.IsUser && p.Name != "Narrator").ToList();

            if (!characters.Any())
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "😔 No available characters.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Clear the list of carousel messages for this chat if it exists, or create a new one
            if (!_carouselMessages.ContainsKey(chatId))
            {
                _carouselMessages[chatId] = new List<int>();
            }
            else
            {
                _carouselMessages[chatId].Clear();
            }

            // Send each character in a separate message with a photo
            foreach (var character in characters)
            {
                if (character == null) continue; // Additional null check

                var keyboard = new InlineKeyboardMarkup(new[]
                {
            new[] { InlineKeyboardButton.WithCallbackData($"Select {character.Name}", $"char_{character.Id}") },
        });

                var captionBuilder = new System.Text.StringBuilder();
                captionBuilder.Append($"**{character.Name}**");
                if (character.CharacterCard?.data?.creator_notes != null) // Null check for data
                {
                    captionBuilder.Append($"\n\n💭 _{character.CharacterCard.data.creator_notes}_"); // Markdown for italic
                }

                string caption = captionBuilder.ToString();
                if (caption.Length > 1024) // Telegram caption size limitation
                {
                    caption = caption.Substring(0, 1020) + "..."; // Trim if too long
                }

                Telegram.Bot.Types.Message sentMessage;

                if (character.Avatar != null && character.Avatar.Length > 0)
                {
                    try
                    {
                        using var stream = new MemoryStream(character.Avatar);
                        sentMessage = await botClient.SendPhoto( // Use SendPhotoAsync
                            chatId: chatId,
                            photo: InputFile.FromStream(stream, $"{character.Name}.jpg"),
                            caption: caption,
                            parseMode: ParseMode.Markdown,
                            replyMarkup: keyboard,
                            cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send photo for character {CharacterName} in chat {ChatId}. Sending text fallback.", character.Name, chatId);
                        // Fallback to text message if photo sending failed
                        sentMessage = await botClient.SendMessage(
                            chatId: chatId,
                            text: caption,
                            parseMode: ParseMode.Markdown,
                            replyMarkup: keyboard,
                            cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    sentMessage = await botClient.SendMessage( // Use SendTextMessageAsync
                        chatId: chatId,
                        text: caption,
                        parseMode: ParseMode.Markdown,
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken);
                }

                // Save the ID of the sent message
                _carouselMessages[chatId].Add(sentMessage.MessageId);
                await Task.Delay(150, cancellationToken); // Short delay between messages
            }
        }

        private async Task<string> Generate(string userMessage)
        {
            try
            {
                if (IsBusy)
                {
                    _logger.LogInformation("Generate called while bot is busy. User message: '{UserMessage}'", userMessage);
                    // You can return a localized message here
                    return "The bot is already responding to your previous message...";
                }

                if (_chatState?.ChatHistory is not null)
                {
                    IsBusy = true;

                    var response = await _queryService.GenerateWithExistingChat(
                        userMessage,
                        _chatState,
                        settingsService,
                        settingsService.User.CloudBasedConfig.UseChatCompletions);

                    if (response.IsSuccess && response.Content != null)
                    {
                        var newMessage = await _chatState.ChatHistory.AddMessage(
                            response.Content,
                            _chatState.ChatHistory.MainCharacter,
                            settingsService.CurrentInstruct);

                        if (newMessage != null)
                        {
                            await newMessage.TranslateMessage(translatorService);
                            IsBusy = false;

                            if (string.IsNullOrEmpty(newMessage.UserNativeLanguageContent))
                                return newMessage.Content;

                            return newMessage.UserNativeLanguageContent;
                        }
                        else
                        {
                            _logger.LogError("Failed to add message to chat history after successful generation. User message: '{UserMessage}'", userMessage);
                            IsBusy = false;
                            return "An error occurred while saving the response.";
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Generation was not successful or content was null. User message: '{UserMessage}'. Reason: {ErrorMessage}",
                            userMessage,
                            response?.ErrorMessage);

                        IsBusy = false;
                        return response?.ErrorMessage ?? "Failed to generate a response. Try rephrasing your message.";
                    }
                }

                _logger.LogInformation("Generate called but no character/chat history is active. User message: '{UserMessage}'", userMessage);
                // You can return a localized message here
                return "You haven't selected a character yet. Use /character to choose one.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Generate method for user message: '{UserMessage}'", userMessage);
                IsBusy = false; // Important to reset flag in case of exception
                                // You can return a localized error message
                return "An error occurred while generating a response.";
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(exception, "Polling error: {ErrorMessage}", errorMessage);


            if (exception is ApiRequestException { ErrorCode: 401 }) // Unauthorized
            {
                _logger.LogCritical("Bot token is invalid or revoked. Stopping the bot.");

                _cts?.Cancel();
            }

            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_cts != null)
            {
                if (IsReceiving)
                {
                    await StopReceivingAsync();
                }
                _cts.Dispose();
                _cts = null;
            }
            _botClient = null;
        }
    }
}