using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ContextBuilderApp.Views;

// 1. Новое перечисление
public enum FilterIconType
{
    None,   // Без иконки (для расширений)
    Folder, // Папка 📁
    File    // Файл 📄
}

public partial class FilterEditor : UserControl
{
    public FilterEditor()
    {
        InitializeComponent();
    }

    // 2. Новое свойство IconType
    public static readonly StyledProperty<FilterIconType> IconTypeProperty =
        AvaloniaProperty.Register<FilterEditor, FilterIconType>(nameof(IconType), FilterIconType.None);

    public FilterIconType IconType
    {
        get => GetValue(IconTypeProperty);
        set => SetValue(IconTypeProperty, value);
    }    

    // --- Свойства заголовка и данных ---

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<FilterEditor, string>(nameof(Title), "Список");

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly StyledProperty<IEnumerable> ItemsSourceProperty =
        AvaloniaProperty.Register<FilterEditor, IEnumerable>(nameof(ItemsSource));

    public IEnumerable ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly StyledProperty<ICommand> RemoveCommandProperty =
        AvaloniaProperty.Register<FilterEditor, ICommand>(nameof(RemoveCommand));

    public ICommand RemoveCommand
    {
        get => GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }

    // --- Режим Текста (для расширений) ---

    public static readonly StyledProperty<bool> IsTextModeProperty =
        AvaloniaProperty.Register<FilterEditor, bool>(nameof(IsTextMode), false);

    public bool IsTextMode
    {
        get => GetValue(IsTextModeProperty);
        set => SetValue(IsTextModeProperty, value);
    }

    public static readonly StyledProperty<string> TextEntryValueProperty =
        AvaloniaProperty.Register<FilterEditor, string>(nameof(TextEntryValue), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public string TextEntryValue
    {
        get => GetValue(TextEntryValueProperty);
        set => SetValue(TextEntryValueProperty, value);
    }

    public static readonly StyledProperty<ICommand> AddTextCommandProperty =
        AvaloniaProperty.Register<FilterEditor, ICommand>(nameof(AddTextCommand));

    public ICommand AddTextCommand
    {
        get => GetValue(AddTextCommandProperty);
        set => SetValue(AddTextCommandProperty, value);
    }

    // --- Режим Диалога (для файлов и папок) ---

    public static readonly StyledProperty<ICommand> AddDialogCommandProperty =
        AvaloniaProperty.Register<FilterEditor, ICommand>(nameof(AddDialogCommand));

    public ICommand AddDialogCommand
    {
        get => GetValue(AddDialogCommandProperty);
        set => SetValue(AddDialogCommandProperty, value);
    }
}