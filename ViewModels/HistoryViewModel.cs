using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using VRN.Services;

namespace VRN.ViewModels;

public class HistoryViewModel : BaseViewModel
{
    private readonly HistoryService _service = new();

    public ObservableCollection<HistoryEntry> Entries { get; } = new();

    private HistoryEntry? _selected;
    public HistoryEntry? Selected
    {
        get => _selected;
        set => SetField(ref _selected, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public RelayCommand ExportCommand { get; }
    public RelayCommand ImportCommand { get; }

    public HistoryViewModel()
    {
        ExportCommand = new RelayCommand(Export, () => Entries.Count > 0);
        ImportCommand = new RelayCommand(Import);
        Reload();
    }

    public void Reload()
    {
        Entries.Clear();
        foreach (var e in _service.Load())
            Entries.Add(e);
    }

    public void AddEntry(HistoryEntry entry)
    {
        _service.Append(entry);
        Entries.Insert(0, entry);
    }

    private void Export()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            FileName = "vrn_history.json",
            DefaultExt = ".json"
        };
        if (dlg.ShowDialog() != true) return;

        _service.ExportTo(dlg.FileName, new List<HistoryEntry>(Entries));
        StatusMessage = "✓ Historial exportado.";
    }

    private void Import()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            DefaultExt = ".json"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var imported = _service.ImportFrom(dlg.FileName);
            foreach (var e in imported)
                Entries.Add(e);
            _service.Save(new List<HistoryEntry>(Entries));
            StatusMessage = $"✓ {imported.Count} entradas importadas.";
        }
        catch
        {
            StatusMessage = "✗ Error al importar historial.";
        }
    }
}