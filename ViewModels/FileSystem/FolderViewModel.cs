using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ContextBuilderApp.ViewModels.FileSystem;

public partial class FolderViewModel : FileSystemNodeViewModel
{
    private readonly bool _includeFiles;
    private bool _hasLoaded = false; // Флаг: загружали ли мы уже эту папку?

    [ObservableProperty]
    private bool _isExpanded;

    // Коллекция хранит БАЗОВЫЙ тип, чтобы мы могли класть туда и файлы, и папки, и статусы
    public ObservableCollection<FileSystemNodeViewModel> Children { get; } = new();

    public FolderViewModel(string path, bool includeFiles) 
        : base(Path.GetFileName(path), path)
    {
        _includeFiles = includeFiles;

        // Добавляем фиктивный статус "Загрузка" сразу.
        // Это заставляет TreeView показать стрелочку раскрытия, так как папка не пуста.
        Children.Add(new StatusViewModel("Загрузка...", StatusType.Loading));
    }

    // Триггер при раскрытии
    partial void OnIsExpandedChanged(bool value)
    {
        if (value && !_hasLoaded)
        {
            _hasLoaded = true; // Запоминаем, что начали загрузку
            _ = LoadChildrenAsync();
        }
    }

    private async Task LoadChildrenAsync()
    {
        // 1. Убеждаемся, что висит статус "Загрузка" (если вдруг мы перезагружаем)
        if (Children.Count != 1 || Children[0] is not StatusViewModel)
        {
            Children.Clear();
            Children.Add(new StatusViewModel("Загрузка данных...", StatusType.Loading));
        }

        try
        {
            // 2. Идем в фон за данными
            var (folders, files) = await Task.Run(() => GetEntries());

            // 3. Возвращаемся в UI и обновляем коллекцию
            Children.Clear();

            if (folders.Count == 0 && files.Count == 0)
            {
                Children.Add(new StatusViewModel("Папка пуста", StatusType.Empty));
                return;
            }

            // Сначала папки
            foreach (var path in folders)
            {
                Children.Add(new FolderViewModel(path, _includeFiles));
            }

            // Потом файлы
            foreach (var path in files)
            {
                Children.Add(new FileViewModel(path));
            }
        }
        catch (UnauthorizedAccessException)
        {
            Children.Clear();
            Children.Add(new StatusViewModel("Нет доступа", StatusType.NoAccess));
        }
        catch (Exception ex)
        {
            Children.Clear();
            Children.Add(new StatusViewModel($"Ошибка: {ex.Message}", StatusType.Error));
        }
    }

    // Логика чтения диска (выполняется в Task.Run)
    private (List<string> Folders, List<string> Files) GetEntries()
    {
        // === ИМИТАЦИЯ МЕДЛЕННОГО ДИСКА ===
        // Замораживаем фоновый поток на 1 секунду (1000 мс)
        // System.Threading.Thread.Sleep(1000); 
        // =================================

        var folders = new List<string>();
        var files = new List<string>();

        var info = new DirectoryInfo(FullPath);

        foreach (var dir in info.GetDirectories())
        {
            if (!dir.Attributes.HasFlag(FileAttributes.Hidden))
                folders.Add(dir.FullName);
        }

        if (_includeFiles)
        {
            foreach (var file in info.GetFiles())
            {
                if (!file.Attributes.HasFlag(FileAttributes.Hidden))
                    files.Add(file.FullName);
            }
        }

        return (folders, files);
    }
}