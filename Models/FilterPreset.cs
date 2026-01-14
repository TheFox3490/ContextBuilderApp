using System.Collections.Generic;

namespace ContextBuilderApp.Models;

public class FilterPreset
{
    public string Name { get; set; } = "Unnamed Preset";
    public List<string> IgnoredFolders { get; set; } = new();
    public List<string> IgnoredFiles { get; set; } = new();
    public List<string> IgnoredExtensions { get; set; } = new();
}