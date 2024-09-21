namespace MousyHub.Components.Models.Model
{
    public class MessageResponse
    {
        // Поле для хранения контента сообщения
        public string Content { get; set; } = "";

        // Поле для обозначения успешности операции
        public bool IsSuccess { get; set; }

        // Поле для хранения сообщений об ошибках
        public string ErrorMessage { get; set; }

        // Конструктор по умолчанию
        public MessageResponse() { }

        // Конструктор с параметрами
        public MessageResponse(string content, bool isSuccess, string errorMessage)
        {
            Content = content;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }
    }
}
