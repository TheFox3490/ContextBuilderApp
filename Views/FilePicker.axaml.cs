using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace ContextBuilderApp.Views;

public partial class FilePicker : UserControl
{
    public FilePicker()
    {
        InitializeComponent();
    }

    // 1. Path: Добавлен DefaultBindingMode.TwoWay
    public static readonly StyledProperty<string?> PathProperty =
        AvaloniaProperty.Register<FilePicker, string?>(
            nameof(Path), 
            defaultBindingMode: BindingMode.TwoWay); // <-- ВАЖНО

    public string? Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public static readonly StyledProperty<bool> IsFolderModeProperty =
        AvaloniaProperty.Register<FilePicker, bool>(nameof(IsFolderMode));

    public bool IsFolderMode
    {
        get => GetValue(IsFolderModeProperty);
        set => SetValue(IsFolderModeProperty, value);
    }

    public static readonly StyledProperty<string> WatermarkProperty =
        AvaloniaProperty.Register<FilePicker, string>(nameof(Watermark), "Выберите...");

    public string Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }
    
    // 2. Новое свойство: Заголовок диалога
    public static readonly StyledProperty<string> DialogTitleProperty =
        AvaloniaProperty.Register<FilePicker, string>(nameof(DialogTitle), "Обзор");

    public string DialogTitle
    {
        get => GetValue(DialogTitleProperty);
        set => SetValue(DialogTitleProperty, value);
    }

    // 3. Новое свойство: Фильтры файлов (например "Image Files|*.jpg;*.png")
    // Для простоты оставим строку, но лучше передавать List<FilePickerFileType>
    public static readonly StyledProperty<string?> FileFilterProperty =
        AvaloniaProperty.Register<FilePicker, string?>(nameof(FileFilter));

    public string? FileFilter
    {
        get => GetValue(FileFilterProperty);
        set => SetValue(FileFilterProperty, value);
    }

    private async void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        // Получаем TopLevel безопасным способом
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        if (IsFolderMode)
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = DialogTitle, // Используем свойство
                AllowMultiple = false
            });

            if (folders.Count >= 1)
            {
                Path = folders[0].TryGetLocalPath();
            }
        }
        else
        {
            var options = new FilePickerOpenOptions
            {
                Title = DialogTitle, // Используем свойство
                AllowMultiple = false
            };

            // Простая логика добавления фильтра, если он задан
            if (!string.IsNullOrEmpty(FileFilter))
            {
                options.FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("Files") { Patterns = new[] { FileFilter } },
                    FilePickerFileTypes.All
                };
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

            if (files.Count >= 1)
            {
                Path = files[0].TryGetLocalPath();
            }
        }
    }
}