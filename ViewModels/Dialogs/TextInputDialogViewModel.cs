using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ContextBuilderApp.ViewModels.Dialogs;

public partial class TextInputDialogViewModel : ViewModelBase
{
    private readonly Action<string?> _onResult;
    private readonly Func<string, string?>? _validator;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))] // <-- Обновляет состояние кнопки при вводе
    private string _inputText = "";

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))] // <-- Обновляет состояние кнопки при появлении ошибки
    private string? _errorMessage;

    // Свойство-помощник для скрытия/показа текста ошибки в UI
    public bool HasErrors => !string.IsNullOrEmpty(ErrorMessage);

    public TextInputDialogViewModel(string title, string initialText, Func<string, string?>? validator, Action<string?> onResult)
    {
        _title = title;
        _inputText = initialText;
        _validator = validator;
        _onResult = onResult;

        // Запускаем валидацию сразу при открытии (чтобы кнопка была неактивна, если поле пустое)
        ValidateInput();
    }

    // Этот метод генерируется CommunityToolkit автоматически для свойства InputText
    partial void OnInputTextChanged(string value)
    {
        ValidateInput();
    }

    private void ValidateInput()
    {
        // 1. Сначала проверяем на пустоту
        if (string.IsNullOrWhiteSpace(InputText))
        {
            // Можно писать "Поле не может быть пустым", а можно просто блокировать кнопку без спама ошибками,
            // если поле пустое. Но для наглядности очистим ошибку, просто кнопка будет неактивна.
            ErrorMessage = null; 
            return;
        }

        // 2. Внешняя валидация (проверка на дубликаты)
        if (_validator != null)
        {
            ErrorMessage = _validator(InputText);
        }
        else
        {
            ErrorMessage = null;
        }
    }

    // Условие активности кнопки ОК
    private bool CanConfirm()
    {
        // Кнопка активна ТОЛЬКО если текст не пустой И нет ошибок валидации
        return !string.IsNullOrWhiteSpace(InputText) && string.IsNullOrEmpty(ErrorMessage);
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        // Тут повторная проверка уже не нужна, так как CanExecute не даст нажать кнопку
        _onResult?.Invoke(InputText);
    }

    [RelayCommand]
    private void Cancel()
    {
        _onResult?.Invoke(null);
    }
}