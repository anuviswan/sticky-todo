using System.IO;
using Microsoft.Win32;
using StickyDo.Widget.Interfaces;

namespace StickyDo.Widget.Services;

/// <summary>
/// WPF implementation of <see cref="IFilePickerService"/> using <see cref="SaveFileDialog"/>.
/// </summary>
public class FilePickerService : IFilePickerService
{
    public string? ShowSaveFileDialog(string defaultFileName, string filter, string? initialDirectory = null)
    {
        var dialog = new SaveFileDialog
        {
            FileName = defaultFileName,
            Filter = filter,
            AddExtension = true
        };

        if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            dialog.InitialDirectory = initialDirectory;

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
