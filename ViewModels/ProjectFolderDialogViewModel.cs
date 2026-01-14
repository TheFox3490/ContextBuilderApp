using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContextBuilderApp.ViewModels.FileSystem;

namespace ContextBuilderApp.ViewModels;

public enum FolderDialogMode { FoldersOnly, FilesOnly }

public partial class ProjectFolderDialogViewModel : ViewModelBase
{
    private readonly Action<string?> _onResult;
    private readonly FolderDialogMode _mode;
    
    // 1. Сохраняем путь корня, чтобы сравнивать с ним
    private readonly string _rootPath; 

    public ObservableCollection<FileSystemNodeViewModel> RootNodes { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private FileSystemNodeViewModel? _selectedNode;

    public ProjectFolderDialogViewModel(string rootPath, FolderDialogMode mode, Action<string?> onResult)
    {
        _rootPath = rootPath; // <-- Запоминаем
        _onResult = onResult;
        _mode = mode;

        if (!string.IsNullOrEmpty(rootPath))
        {
            bool showFiles = (_mode == FolderDialogMode.FilesOnly);
            
            var root = new FolderViewModel(rootPath, showFiles);
            root.IsExpanded = true; 
            
            RootNodes.Add(root);
        }
    }

    private bool CanConfirm()
    {
        if (SelectedNode == null) return false;

        // Нельзя выбирать статус (Загрузка/Ошибка)
        if (SelectedNode is StatusViewModel) return false;

        // 2. НОВАЯ ПРОВЕРКА: Если выбранный путь совпадает с корнем проекта - блокируем
        // StringComparison.OrdinalIgnoreCase на случай разного регистра букв диска в Windows
        if (string.Equals(SelectedNode.FullPath, _rootPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_mode == FolderDialogMode.FoldersOnly)
        {
            return SelectedNode is FolderViewModel;
        }
        else 
        {
            return SelectedNode is FileViewModel;
        }
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        _onResult?.Invoke(SelectedNode?.FullPath);
    }

    [RelayCommand]
    private void Cancel()
    {
        _onResult?.Invoke(null);
    }
}