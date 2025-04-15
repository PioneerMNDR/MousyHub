namespace MousyHub.Models.Model
{
    public class MessageResponse
    {
        // Поле для хранения контента сообщения
        public string Content { get; set; } = "";

        public string ReasoningContent { get; set; } = "";

        // Поле для обозначения успешности операции
        public bool IsSuccess { get; set; }

        // Поле для хранения сообщений об ошибках
        public string ErrorMessage { get; set; } = string.Empty;

        public string RawData { get; set; } = string.Empty;

        // Конструктор по умолчанию
        public MessageResponse() { }

        // Конструктор с параметрами
        public MessageResponse(string content, bool isSuccess, string errorMessage)
        {
            Content = content;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }
        public MessageResponse(string content, bool isSuccess, string errorMessage, string reasoningContent, string rawData)
        {
            Content = content;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            ReasoningContent = reasoningContent;
            RawData = rawData;
        }
    }
}
