using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ContextBuilderApp.Models;

namespace ContextBuilderApp.Services;

public class PresetService
{
    // Имя папки в AppData (лучше называть именем продукта)
    private const string AppFolderName = "ContextBuilder"; 
    private const string ConfigFileName = "presets.json";

    /// <summary>
    /// Возвращает полный путь к файлу конфигурации.
    /// Например: C:\Users\User\AppData\Roaming\ContextBuilder\presets.json
    /// </summary>
    private string GetConfigFilePath()
    {
        // Получаем путь к стандартной папке данных приложений пользователя
        // Windows: C:\Users\<User>\AppData\Roaming
        // Linux/Mac: ~/.config (обычно)
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        
        // Собираем полный путь
        return Path.Combine(appDataPath, AppFolderName, ConfigFileName);
    }

    public List<FilterPreset> LoadPresets()
    {
        var filePath = GetConfigFilePath();

        // Если файла нет, создаем дефолтные настройки
        if (!File.Exists(filePath))
        {
            var defaults = CreateDefaultPresets();
            // Сразу пытаемся сохранить их на диск, чтобы файл появился
            SavePresets(defaults); 
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var result = JsonSerializer.Deserialize<List<FilterPreset>>(json);
            return result ?? CreateDefaultPresets();
        }
        catch (Exception)
        {
            // Если JSON поврежден, возвращаем дефолт, но не перезаписываем файл сразу,
            // чтобы пользователь мог попробовать его починить вручную, если захочет.
            return CreateDefaultPresets();
        }
    }

    public void SavePresets(List<FilterPreset> presets)
    {
        try
        {
            var filePath = GetConfigFilePath();
            var directory = Path.GetDirectoryName(filePath);

            // ВАЖНО: Убеждаемся, что папка существует, прежде чем писать файл
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(presets, options);
            
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            // В реальном приложении тут можно залогировать ошибку или показать MessageBox,
            // но так как это сервис, просто выводим в консоль отладки.
            System.Diagnostics.Debug.WriteLine($"Failed to save presets: {ex.Message}");
        }
    }

    private List<FilterPreset> CreateDefaultPresets()
    {
        return new List<FilterPreset>
        {
            new FilterPreset
            {
                Name = "Default Web",
                IgnoredFolders = { "node_modules", ".git", "dist", "build", ".idea", ".vscode", "coverage" },
                IgnoredFiles = { "package-lock.json", "yarn.lock", ".env" },
                IgnoredExtensions = { ".exe", ".dll", ".png", ".jpg", ".jpeg", ".ico", ".svg", ".zip", ".tar" }
            },
            new FilterPreset
            {
                Name = "C# .NET",
                IgnoredFolders = { "bin", "obj", ".git", ".vs", ".idea", ".vscode", "TestResults" },
                IgnoredFiles = { "*.user", "*.suo" },
                IgnoredExtensions = { ".exe", ".dll", ".pdb", ".nupkg", ".ico", ".lock" }
            }
        };
    }
}