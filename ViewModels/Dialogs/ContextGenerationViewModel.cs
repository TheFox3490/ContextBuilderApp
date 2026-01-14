using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContextBuilderApp.Models;
using ContextBuilderApp.Services;

namespace ContextBuilderApp.ViewModels.Dialogs;

public partial class ContextGenerationViewModel : ViewModelBase
{
    private readonly string _rootPath;
    private readonly FilterPreset _preset;
    private readonly Action _onClose;
    private readonly ContextGeneratorService _service;

    // --- Основное состояние ---

    [ObservableProperty]
    // ВАЖНО: Эти атрибуты заставляют кнопки перепроверить свою доступность после завершения генерации
    [NotifyCanExecuteChangedFor(nameof(CopyToClipboardCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveToFileCommand))]
    private bool _isGenerating = true;

    [ObservableProperty]
    private string _loadingMessage = "Анализ файлов...";

    [ObservableProperty]
    private ContextResult? _result;

    // --- UI Состояние для кнопок (блокировка при анимации/сохранении) ---

    [ObservableProperty]
    // Эти атрибуты блокируют кнопки, когда мы сами этого хотим (например, на 3 сек после копирования)
    [NotifyCanExecuteChangedFor(nameof(CopyToClipboardCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveToFileCommand))]
    private bool _isButtonsLocked = false; 

    [ObservableProperty]
    private string _copyButtonText = "Копировать в буфер";

    public ContextGenerationViewModel(string rootPath, FilterPreset preset, Action onClose)
    {
        _rootPath = rootPath;
        _preset = preset;
        _onClose = onClose;
        _service = new ContextGeneratorService();

        // Запускаем процесс сразу при создании VM
        _ = GenerateProcess();
    }

    private async Task GenerateProcess()
    {
        IsGenerating = true;
        // Даем UI время на отрисовку окна перед стартом тяжелой задачи
        await Task.Delay(100);

        try
        {
            Result = await _service.GenerateAsync(_rootPath, _preset);
        }
        catch (Exception ex)
        {
            Result = new ContextResult 
            { 
                TreePreview = $"Ошибка генерации: {ex.Message}",
                FullContent = $"Error: {ex.Message}"
            };
        }
        finally
        {
            IsGenerating = false; // <-- Здесь кнопки разблокируются благодаря NotifyCanExecuteChangedFor
        }
    }

    // Вспомогательный метод для получения доступа к окну (для буфера и диалогов)
    private TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return TopLevel.GetTopLevel(desktop.MainWindow);
        }
        return null;
    }

    // Условие доступности кнопок: НЕ идет генерация И НЕ включена временная блокировка
    private bool CanInteract()
    {
        return !IsGenerating && !IsButtonsLocked;
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private async Task CopyToClipboard()
    {
        if (Result == null) return;
        var topLevel = GetTopLevel();
        if (topLevel?.Clipboard == null) return;

        try
        {
            // 1. Копируем текст
            await topLevel.Clipboard.SetTextAsync(Result.FullContent);

            // 2. Визуальная реакция
            IsButtonsLocked = true; // Блокируем нажатия
            var originalText = CopyButtonText;
            CopyButtonText = "Скопировано! ✅";

            // 3. Ждем 3 секунды
            await Task.Delay(3000);

            // 4. Возвращаем как было
            CopyButtonText = originalText;
        }
        finally
        {
            IsButtonsLocked = false; // Разблокируем
        }
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private async Task SaveToFile()
    {
        if (Result == null) return;
        var topLevel = GetTopLevel();
        if (topLevel == null) return;

        IsButtonsLocked = true; // Блокируем интерфейс на время выбора файла

        try
        {
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить контекст в файл",
                DefaultExtension = ".txt",
                SuggestedFileName = $"context_{DateTime.Now:yyyyMMdd_HHmm}.txt",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Text files") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("All files") { Patterns = new[] { "*.*" } }
                }
            });

            if (file != null)
            {
                using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                await writer.WriteAsync(Result.FullContent);
            }
        }
        catch (Exception)
        {
            // Игнорируем ошибки сохранения (или можно показать MessageBox)
        }
        finally
        {
            IsButtonsLocked = false;
        }
    }

    [RelayCommand]
    private void Close() => _onClose();
}