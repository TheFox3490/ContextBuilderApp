using CommunityToolkit.Mvvm.ComponentModel;

namespace ContextBuilderApp.ViewModels.FileSystem;

public abstract class FileSystemNodeViewModel : ObservableObject
{
    public string Name { get; }
    public string FullPath { get; }

    protected FileSystemNodeViewModel(string name, string fullPath)
    {
        Name = name;
        FullPath = fullPath;
    }
}