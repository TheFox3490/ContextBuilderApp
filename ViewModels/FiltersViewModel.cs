using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized; // Для CollectionChanged
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContextBuilderApp.Models;
using ContextBuilderApp.Services;

namespace ContextBuilderApp.ViewModels;

public partial class FiltersViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly PresetService _presetService;
    
    // Флаг, чтобы отличать загрузку данных (ApplyPreset) от действий пользователя
    private bool _isLoading = false;

    // --- Пресеты ---
    public ObservableCollection<FilterPreset> Presets { get; } = new();

    [ObservableProperty]
    private FilterPreset? _selectedPreset;

    // --- Индикатор изменений ---
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RevertChangesCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCurrentPresetCommand))]
    private bool _hasUnsavedChanges;

    // --- Коллекции данных (Текущее состояние UI) ---
    public ObservableCollection<string> IgnoredFolders { get; } = new();
    public ObservableCollection<string> IgnoredFiles { get; } = new();
    public ObservableCollection<string> IgnoredExtensions { get; } = new();

    // --- Свойства для UI ---
    [ObservableProperty]
    private string _newExtensionText = "";

    public FiltersViewModel(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        _presetService = new PresetService();

        // Подписываемся на изменения коллекций один раз в конструкторе
        IgnoredFolders.CollectionChanged += OnCollectionChanged;
        IgnoredFiles.CollectionChanged += OnCollectionChanged;
        IgnoredExtensions.CollectionChanged += OnCollectionChanged;

        LoadPresets();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Если изменение происходит программно (при смене пресета), игнорируем
        if (_isLoading) return;
        
        CheckIfDirty();
    }

    // Метод проверки: отличаются ли данные в UI от сохраненного пресета
    private void CheckIfDirty()
    {
        if (SelectedPreset == null)
        {
            HasUnsavedChanges = false;
            return;
        }

        bool foldersChanged = !AreListsEqual(SelectedPreset.IgnoredFolders, IgnoredFolders);
        bool filesChanged = !AreListsEqual(SelectedPreset.IgnoredFiles, IgnoredFiles);
        bool extsChanged = !AreListsEqual(SelectedPreset.IgnoredExtensions, IgnoredExtensions);

        HasUnsavedChanges = foldersChanged || filesChanged || extsChanged;
    }

    // Хелпер для сравнения списков (игнорируя порядок элементов)
    private bool AreListsEqual(List<string> original, ObservableCollection<string> current)
    {
        if (original.Count != current.Count) return false;
        
        // Используем HashSet для быстрого сравнения без учета порядка
        var setOriginal = new HashSet<string>(original);
        var setCurrent = new HashSet<string>(current);
        
        return setOriginal.SetEquals(setCurrent);
    }

    private void LoadPresets()
    {
        var loaded = _presetService.LoadPresets();
        Presets.Clear();
        foreach (var p in loaded) Presets.Add(p);

        if (Presets.Count > 0)
        {
            SelectedPreset = Presets[0];
        }
    }

    partial void OnSelectedPresetChanged(FilterPreset? value)
    {
        if (value != null)
        {
            ApplyPresetToUi(value);
        }
    }

    private void ApplyPresetToUi(FilterPreset preset)
    {
        _isLoading = true; // Начало программной загрузки

        try
        {
            IgnoredFolders.Clear();
            foreach (var item in preset.IgnoredFolders) IgnoredFolders.Add(item);

            IgnoredFiles.Clear();
            foreach (var item in preset.IgnoredFiles) IgnoredFiles.Add(item);

            IgnoredExtensions.Clear();
            foreach (var item in preset.IgnoredExtensions) IgnoredExtensions.Add(item);
        }
        finally
        {
            _isLoading = false; // Конец загрузки
        }

        // После загрузки пресета состояние "чистое"
        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Сбрасывает UI‑коллекции к состоянию выбранного пресета
    /// (или полностью очищает их, если пресет не выбран) и снимает индикатор «не сохранено».
    /// </summary>
    public void ResetToSavedState()
    {
        _isLoading = true;
        try
        {
            if (SelectedPreset != null)
            {
                // Папки
                IgnoredFolders.Clear();
                foreach (var f in SelectedPreset.IgnoredFolders) IgnoredFolders.Add(f);

                // Файлы
                IgnoredFiles.Clear();
                foreach (var f in SelectedPreset.IgnoredFiles) IgnoredFiles.Add(f);

                // Расширения
                IgnoredExtensions.Clear();
                foreach (var e in SelectedPreset.IgnoredExtensions) IgnoredExtensions.Add(e);
            }
            else
            {
                IgnoredFolders.Clear();
                IgnoredFiles.Clear();
                IgnoredExtensions.Clear();
            }

            NewExtensionText = string.Empty;
            HasUnsavedChanges = false;
        }
        finally
        {
            _isLoading = false;
        }
    }    

    // --- Команды ---

    [RelayCommand(CanExecute = nameof(HasUnsavedChanges))] // Кнопка активна только если есть изменения
    private void SaveCurrentPreset()
    {
        if (SelectedPreset == null) return;

        // 1. Обновляем модель
        SelectedPreset.IgnoredFolders = IgnoredFolders.ToList();
        SelectedPreset.IgnoredFiles = IgnoredFiles.ToList();
        SelectedPreset.IgnoredExtensions = IgnoredExtensions.ToList();

        // 2. Сохраняем на диск
        _presetService.SavePresets(Presets.ToList());

        // 3. Сбрасываем флаг изменений
        HasUnsavedChanges = false;
    }

    [RelayCommand(CanExecute = nameof(HasUnsavedChanges))] // Кнопка активна только если есть изменения
    private void RevertChanges()
    {
        if (SelectedPreset != null)
        {
            // Поскольку мы не меняли объект SelectedPreset в памяти (мы меняли только коллекции UI),
            // достаточно просто заново накатить данные из SelectedPreset.
            // Примечание: если нужен Hard Revert (с диска), логика чуть сложнее (см. предыдущий код),
            // но для UI-сброса достаточно этого:
            
            ApplyPresetToUi(SelectedPreset);
        }
    }

    // ... CreateNewPreset, DeleteCurrentPreset и другие команды остаются без изменений ...
    // ... Но CreateNewPreset тоже желательно сбрасывать HasUnsavedChanges после создания ...
    
    [RelayCommand]
    private void CreateNewPreset()
    {
        _mainViewModel.OpenTextInput("Новый пресет", "", (input) => 
        {
             if (Presets.Any(p => p.Name.Equals(input, StringComparison.OrdinalIgnoreCase)))
                return "Пресет с таким именем уже существует.";
             return null;
        },
        (resultName) => 
        {
            if (!string.IsNullOrEmpty(resultName))
            {
                var newPreset = new FilterPreset
                {
                    Name = resultName,
                    IgnoredFolders = IgnoredFolders.ToList(),
                    IgnoredFiles = IgnoredFiles.ToList(),
                    IgnoredExtensions = IgnoredExtensions.ToList()
                };

                Presets.Add(newPreset);
                SelectedPreset = newPreset; 
                _presetService.SavePresets(Presets.ToList());
                
                // Важно:
                HasUnsavedChanges = false; 
            }
        });
    }

    // ... Остальные методы (Delete, Add/Remove) без изменений. Они триггерят CollectionChanged,
    // который сам вызовет CheckIfDirty.
    
    [RelayCommand]
    private void RemoveFolder(string path) => IgnoredFolders.Remove(path);
    [RelayCommand]
    private void RemoveFile(string path) => IgnoredFiles.Remove(path);
    [RelayCommand]
    private void RemoveExtension(string ext) => IgnoredExtensions.Remove(ext);
    
    // В AddExtension и Add...ViaTree ничего менять не надо, 
    // добавление в коллекцию автоматически проверит Dirty State.

    [RelayCommand]
    private void AddExtension()
    {
        if (!string.IsNullOrWhiteSpace(NewExtensionText))
        {
            var ext = NewExtensionText.Trim();
            if (!ext.StartsWith(".")) ext = "." + ext;
            if (!IgnoredExtensions.Contains(ext)) IgnoredExtensions.Add(ext);
            NewExtensionText = "";
        }
    }
    
    public void ClearAll()
    {
        // Очистка тоже вызовет CollectionChanged
        IgnoredFolders.Clear();
        IgnoredFiles.Clear();
        IgnoredExtensions.Clear();
    }
    
    // ... (Методы AddFolderViaTree, AddFileViaTree, GetRelativePath копируются из старого кода) ...
     [RelayCommand]
    private void AddFolderViaTree()
    {
        var projectRoot = _mainViewModel.TargetFolderPath;
        if (string.IsNullOrEmpty(projectRoot) || !Directory.Exists(projectRoot)) return;

        _mainViewModel.OpenProjectDialog(projectRoot, FolderDialogMode.FoldersOnly, (selectedPath) => 
        {
            if (!string.IsNullOrEmpty(selectedPath))
            {
                var relative = GetRelativePath(selectedPath);
                if (!string.IsNullOrWhiteSpace(relative) && relative != "." && !IgnoredFolders.Contains(relative))
                {
                    IgnoredFolders.Add(relative);
                }
            }
        });
    }

    [RelayCommand]
    private void AddFileViaTree()
    {
        var projectRoot = _mainViewModel.TargetFolderPath;
        if (string.IsNullOrEmpty(projectRoot) || !Directory.Exists(projectRoot)) return;

        _mainViewModel.OpenProjectDialog(projectRoot, FolderDialogMode.FilesOnly, (selectedPath) => 
        {
            if (!string.IsNullOrEmpty(selectedPath))
            {
                var relative = GetRelativePath(selectedPath);
                if (!string.IsNullOrWhiteSpace(relative) && !IgnoredFiles.Contains(relative))
                {
                    IgnoredFiles.Add(relative);
                }
            }
        });
    }
    
    // ... GetRelativePath, DeleteCurrentPreset ...
    private string GetRelativePath(string fullPath)
    {
        var root = _mainViewModel.TargetFolderPath;
        if (string.IsNullOrEmpty(root)) return fullPath;
        try
        {
            var relative = Path.GetRelativePath(root, fullPath);
            if (relative.StartsWith("..") || Path.IsPathRooted(relative)) return fullPath; 
            return relative;
        }
        catch { return fullPath; }
    }
    
    [RelayCommand]
    private void DeleteCurrentPreset()
    {
        if (SelectedPreset == null) return;
        if (Presets.Count <= 1) return;

        _mainViewModel.OpenConfirmation(
            "Удаление пресета",
            $"Вы уверены, что хотите удалить пресет '{SelectedPreset.Name}'?",
            (confirmed) => 
            {
                if (confirmed)
                {
                    var toRemove = SelectedPreset;
                    var index = Presets.IndexOf(toRemove);
                    if (index > 0) SelectedPreset = Presets[index - 1];
                    else SelectedPreset = Presets[index + 1];

                    Presets.Remove(toRemove);
                    _presetService.SavePresets(Presets.ToList());
                    HasUnsavedChanges = false;
                }
            });
    }

    public FilterPreset GetCurrentSnapshot()
    {
        return new FilterPreset
        {
            Name = SelectedPreset?.Name ?? "Unsaved Snapshot",
            // ToList() создает копии списков, чтобы они не менялись во время работы генератора
            IgnoredFolders = IgnoredFolders.ToList(),
            IgnoredFiles = IgnoredFiles.ToList(),
            IgnoredExtensions = IgnoredExtensions.ToList()
        };
    }    
}