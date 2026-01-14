using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContextBuilderApp.Models;

namespace ContextBuilderApp.Services;

public class ContextGeneratorService
{
    // Символы для рисования дерева
    private const string Cross = "├── ";
    private const string Corner = "└── ";
    private const string Vertical = "│   ";
    private const string Space = "    ";

    public async Task<ContextResult> GenerateAsync(string rootPath, FilterPreset preset)
    {
        return await Task.Run(() =>
        {
            var sbContent = new StringBuilder();
            var sbTree = new StringBuilder();
            
            var stats = new ContextResult();

            // 1. Заголовок и Дерево
            sbTree.AppendLine("# PROJECT STRUCTURE");
            sbTree.AppendLine("=================");
            sbTree.AppendLine(Path.GetFileName(rootPath) + "/");
            
            // Начинаем рекурсивный обход для построения дерева
            var filesToRead = new List<string>();
            BuildTree(rootPath, rootPath, preset, "", sbTree, filesToRead);
            
            sbTree.AppendLine();
            sbTree.AppendLine("=================");
            
            // Сохраняем дерево отдельно для превью
            stats.TreePreview = sbTree.ToString();

            // 2. Сборка полного контента
            sbContent.Append(stats.TreePreview);
            sbContent.AppendLine();

            foreach (var file in filesToRead)
            {
                try
                {
                    // Пропускаем бинарники (базовая проверка)
                    if (IsBinary(file))
                    {
                        AppendHeader(sbContent, rootPath, file, "(Binary file skipped)");
                        continue;
                    }

                    var text = File.ReadAllText(file);
                    AppendHeader(sbContent, rootPath, file);
                    sbContent.AppendLine(text);
                    sbContent.AppendLine(); // Пустая строка между файлами
                    
                    stats.FileCount++;
                    stats.TotalBytes += text.Length;
                }
                catch (Exception ex)
                {
                    AppendHeader(sbContent, rootPath, file, $"(Error reading file: {ex.Message})");
                }
            }

            stats.FullContent = sbContent.ToString();
            // Грубая оценка токенов для английского текста ~4 символа на токен
            stats.EstimatedTokens = stats.FullContent.Length / 4; 

            return stats;
        });
    }

    private void BuildTree(string rootPath, string currentPath, FilterPreset preset, string indent, StringBuilder sb, List<string> filesCollector)
    {
        var info = new DirectoryInfo(currentPath);
        
        // Получаем все записи и фильтруем их сразу
        var entries = info.GetFileSystemInfos()
            .Where(e => !IsIgnored(e, rootPath, preset))
            .OrderBy(e => e.Name) // Сортировка по алфавиту, как в tree
            .ToList();

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var isLast = i == entries.Count - 1;

            // Рисуем ветку
            sb.Append(indent);
            sb.Append(isLast ? Corner : Cross);
            sb.AppendLine(entry.Name);

            if (entry is DirectoryInfo subDir)
            {
                // Рекурсия: увеличиваем отступ. 
                // Если текущая папка последняя, то ставим пробелы, иначе вертикальную черту.
                var childIndent = indent + (isLast ? Space : Vertical);
                BuildTree(rootPath, subDir.FullName, preset, childIndent, sb, filesCollector);
            }
            else
            {
                // Если файл - добавляем в список на чтение
                filesCollector.Add(entry.FullName);
            }
        }
    }

    private bool IsIgnored(FileSystemInfo info, string rootPath, FilterPreset preset)
    {
        // 1. Скрытые файлы ОС
        if (info.Attributes.HasFlag(FileAttributes.Hidden)) return true;

        var relativePath = Path.GetRelativePath(rootPath, info.FullName);
        
        // Приводим слэши к единому виду для сравнения
        var normalizedPath = relativePath.Replace("\\", "/");

        // 2. Проверка папок
        // Проверяем два варианта:
        // А) Точное совпадение имени папки (например "node_modules" в любом месте пути)
        // Б) Совпадение относительного пути (например "Client/build")
        
        // Разбиваем путь на части для поиска имен папок
        var parts = normalizedPath.Split('/');
        
        foreach (var ignored in preset.IgnoredFolders)
        {
            var normalizedIgnored = ignored.Replace("\\", "/");

            // Проверка А: Если имя любой родительской папки совпадает с игнорируемой
            // StringComparison.OrdinalIgnoreCase решает проблему с регистром (Bin vs bin)
            if (parts.Any(p => p.Equals(normalizedIgnored, StringComparison.OrdinalIgnoreCase))) 
                return true;

            // Проверка Б: Если путь начинается с игнорируемого пути (для вложенных фильтров типа "src/temp")
            if (normalizedPath.StartsWith(normalizedIgnored + "/", StringComparison.OrdinalIgnoreCase) || 
                normalizedPath.Equals(normalizedIgnored, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (info is FileInfo)
        {
            // 3. Проверка по имени файла (игнорируем регистр)
            if (preset.IgnoredFiles.Any(f => f.Equals(info.Name, StringComparison.OrdinalIgnoreCase))) 
                return true;

            // 3.1 Также полезно проверять полный относительный путь для файлов (например "src/config.local.js")
            if (preset.IgnoredFiles.Any(f => normalizedPath.Equals(f.Replace("\\", "/"), StringComparison.OrdinalIgnoreCase)))
                return true;
            
            // 4. Проверка по расширению (игнорируем регистр)
            if (preset.IgnoredExtensions.Any(e => e.Equals(info.Extension, StringComparison.OrdinalIgnoreCase))) 
                return true;
        }

        return false;
    }

    private void AppendHeader(StringBuilder sb, string root, string filePath, string? note = null)
    {
        var rel = Path.GetRelativePath(root, filePath).Replace("\\", "/");
        sb.AppendLine($"# FILE: {rel} {note ?? ""}");
    }

    // ==========================================
    // ЛОГИКА ОПРЕДЕЛЕНИЯ БИНАРНЫХ ФАЙЛОВ
    // ==========================================

    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Изображения
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".tiff", ".tif", ".webp", ".svgz", ".heic", ".raw", ".psd", ".ai",
        
        // Аудио
        ".mp3", ".wav", ".aac", ".wma", ".ogg", ".flac", ".m4a", ".mid", ".midi",
        
        // Видео
        ".mp4", ".avi", ".mov", ".wmv", ".mkv", ".flv", ".webm", ".m4v", ".3gp", ".mpeg", ".mpg",
        
        // Документы (офисные форматы - это zip-архивы)
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".ods", ".odp", ".rtf",
        
        // Архивы и сжатие
        ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".cab", ".jar", ".war", ".ear",
        
        // Исполняемые файлы и библиотеки
        ".exe", ".dll", ".so", ".dylib", ".bin", ".obj", ".o", ".a", ".lib", ".msi", ".com", ".sys", ".drv", ".cpl", ".scr",
        
        // Базы данных
        ".sqlite", ".sqlite3", ".db", ".db3", ".mdb", ".accdb", ".mdf", ".ldf", ".ibd", ".frm", ".pdb", // pdb - debug symbols
        
        // Байт-код и промежуточные файлы
        ".class", ".pyc", ".pyo", ".elc", ".suo",
        
        // Шрифты
        ".ttf", ".otf", ".woff", ".woff2", ".eot",
        
        // Git
        ".pack", ".idx",
        
        // Разное
        ".ds_store", ".thumbs.db", ".dat", ".lnk", ".swf", ".crx", ".nes", ".rom"
    };

    private bool IsBinary(string filePath)
    {
        // 1. Быстрая проверка по расширению
        var ext = Path.GetExtension(filePath);

        // Если расширение есть И оно в списке бинарных — сразу возвращаем true
        if (!string.IsNullOrEmpty(ext) && BinaryExtensions.Contains(ext))
        {
            return true;
        }

        // 2. Проверка содержимого (читаем начало файла)
        // Сюда мы попадем в двух случаях:
        // а) Расширения нет вообще (например "Dockerfile")
        // б) Расширение есть, но мы его не знаем (например ".mycustomext")
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            
            var buffer = new byte[8192];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);

            if (bytesRead == 0) return false; // Пустой файл = текст

            for (int i = 0; i < bytesRead; i++)
            {
                if (buffer[i] == 0) return true; // Нашли NULL-байт = бинарник
            }

            return false;
        }
        catch (IOException)
        {
            // Если не смогли прочитать (нет прав, файл занят) — считаем бинарным от греха подальше
            return true; 
        }
    }
}