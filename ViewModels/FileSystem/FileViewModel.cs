namespace ContextBuilderApp.ViewModels.FileSystem;

public class FileViewModel : FileSystemNodeViewModel
{
    public FileViewModel(string fullPath) 
        : base(System.IO.Path.GetFileName(fullPath), fullPath)
    {
    }
}