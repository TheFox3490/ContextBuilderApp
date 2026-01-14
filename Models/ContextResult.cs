namespace ContextBuilderApp.Models;

public class ContextResult
{
    public string FullContent { get; set; } = ""; // Весь текст (дерево + файлы)
    public string TreePreview { get; set; } = ""; // Только дерево (для показа в UI)
    public int FileCount { get; set; }
    public long TotalBytes { get; set; }
    public int EstimatedTokens { get; set; } // Грубая оценка (символы / 4)
}