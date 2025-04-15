namespace MousyHub.Classes.User
{
    /// <summary>
    /// Класс для управления настройками "рассуждений" (reasoning) в чат-системе.
    /// Class for managing reasoning settings in a chat system.
    /// </summary>
    public class ReasoningOptions
    {

        public bool Enabled { get; set; } = false;
        /// <summary>
        /// Автоматически извлекать reasoning из текста сообщений.
        /// Automatically parse reasoning from message text.
        /// </summary>
        public bool AutoParse { get; set; } = false;

        /// <summary>
        /// Добавлять reasoning в промты (запросы к модели).
        /// Add reasoning to prompts (model queries).
        /// </summary>
        public bool AddToPrompts { get; set; } = false;

        /// <summary>
        /// Автоматически разворачивать блоки reasoning при отображении.
        /// Automatically expand reasoning blocks when displayed.
        /// </summary>
        public bool AutoExpand { get; set; } = false;

        /// <summary>
        /// Показывать скрытые reasoning-блоки (если модель их не возвращает явно).
        /// Show hidden reasoning blocks (if model doesn't return them explicitly).
        /// </summary>
        public bool ShowHidden { get; set; } = false;

        /// <summary>
        /// Префикс для форматирования reasoning-блоков.
        /// Prefix for formatting reasoning blocks.
        /// </summary>
        public string Prefix { get; set; } = "<think>\n";

        /// <summary>
        /// Суффикс для форматирования reasoning-блоков.
        /// Suffix for formatting reasoning blocks.
        /// </summary>
        public string Suffix { get; set; } = "\n</think>";

        /// <summary>
        /// Разделитель между reasoning и основным текстом сообщения.
        /// Separator between reasoning and main message text.
        /// </summary>
        public string Separator { get; set; } = "\n\n";

    }
}
