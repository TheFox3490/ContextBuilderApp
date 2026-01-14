using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ContextBuilderApp.ViewModels.Dialogs;
using ContextBuilderApp.Services;

namespace ContextBuilderApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _targetFilePath = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProjectLoaded))]
    private string _targetFolderPath = "";

    // -- Управление оверлеем --
    
    [ObservableProperty]
    private bool _isOverlayVisible;

    [ObservableProperty]
    private ViewModelBase? _overlayViewModel; // Сюда положим VM диалога

    [ObservableProperty]
    private int _selectedTabIndex = 0;    

    public bool IsProjectLoaded => !string.IsNullOrEmpty(TargetFolderPath);

    public FiltersViewModel Filters { get; }

    public MainWindowViewModel()
    {
        Filters = new FiltersViewModel(this);
    }

    // Метод, который будет вызывать FiltersViewModel, чтобы открыть диалог
    public void OpenProjectDialog(string rootPath, FolderDialogMode mode, Action<string?> callback)
    {
        var dialogVm = new ProjectFolderDialogViewModel(rootPath, mode, (resultPath) => 
        {
            IsOverlayVisible = false;
            OverlayViewModel = null;
            callback(resultPath);
        });

        OverlayViewModel = dialogVm;
        IsOverlayVisible = true;
    }

    // Закрыть проект
    [RelayCommand]
    private void CloseProject()
    {
        Filters.ResetToSavedState();

        TargetFolderPath = "";
        TargetFilePath = "";
        SelectedTabIndex = 0;
    }

    public void OpenTextInput(string title, string initialValue, Func<string, string?>? validator, Action<string?> callback)
    {
        var vm = new TextInputDialogViewModel(title, initialValue, validator, (result) => 
        {
            // Скрываем оверлей при закрытии диалога
            IsOverlayVisible = false;
            OverlayViewModel = null;
            callback(result);
        });

        OverlayViewModel = vm;
        IsOverlayVisible = true;
    }

    // Метод для открытия диалога подтверждения
    public void OpenConfirmation(string title, string message, Action<bool> callback)
    {
        var vm = new ConfirmationDialogViewModel(title, message, (result) =>
        {
            IsOverlayVisible = false;
            OverlayViewModel = null;
            callback(result);
        });

        OverlayViewModel = vm;
        IsOverlayVisible = true;
    }

    [RelayCommand]
    private void GenerateContext()
    {
        if (!IsProjectLoaded) return;

        // Получаем актуальный снимок настроек (включая несохраненные изменения)
        var presetToUse = Filters.GetCurrentSnapshot();

        var vm = new ContextGenerationViewModel(
            TargetFolderPath, 
            presetToUse, // Передаем снимок
            () => 
            {
                IsOverlayVisible = false;
                OverlayViewModel = null;
            });

        OverlayViewModel = vm;
        IsOverlayVisible = true;
    }
}