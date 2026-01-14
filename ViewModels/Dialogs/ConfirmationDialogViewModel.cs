using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ContextBuilderApp.ViewModels.Dialogs;

public partial class ConfirmationDialogViewModel : ViewModelBase
{
    private readonly Action<bool> _onResult;

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _message = "";

    public ConfirmationDialogViewModel(string title, string message, Action<bool> onResult)
    {
        _title = title;
        _message = message;
        _onResult = onResult;
    }

    [RelayCommand]
    private void Confirm() => _onResult?.Invoke(true);

    [RelayCommand]
    private void Cancel() => _onResult?.Invoke(false);
}