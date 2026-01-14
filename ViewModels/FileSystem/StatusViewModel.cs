namespace ContextBuilderApp.ViewModels.FileSystem;

public enum StatusType
{
    Loading,
    Empty,
    Error,
    NoAccess
}

public class StatusViewModel : FileSystemNodeViewModel
{
    public StatusType Type { get; }

    // Конструктор принимает сообщение и тип
    public StatusViewModel(string message, StatusType type) 
        : base(message, string.Empty) // Путь пустой, это не файл
    {
        Type = type;
    }
}