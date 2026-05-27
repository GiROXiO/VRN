using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using VRN.Models;

namespace VRN.Services;

public class HistoryEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [JsonPropertyName("parameters")]
    public CodeParameters Parameters { get; set; } = new();

    [JsonPropertyName("result")]
    public CodeResult Result { get; set; } = new();

    [JsonIgnore]
    public string DisplayLabel => $"RS({Parameters.N},{Parameters.K}) GF({Parameters.Q}) — {Timestamp:HH:mm dd/MM/yy}";
}

public class HistoryService
{
    private static readonly string FolderPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VRN");

    private static readonly string FilePath = Path.Combine(FolderPath, "history.json");

    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
        Converters = { new Int2DArrayConverter() }
    };

    public List<HistoryEntry> Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new List<HistoryEntry>();
            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<HistoryEntry>>(json, _opts) ?? new List<HistoryEntry>();
        }
        catch
        {
            return new List<HistoryEntry>();
        }
    }

    public void Save(List<HistoryEntry> entries)
    {
        Directory.CreateDirectory(FolderPath);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(entries, _opts));
    }

    public void Append(HistoryEntry entry)
    {
        var entries = Load();
        entries.Insert(0, entry);
        if (entries.Count > 200) entries.RemoveRange(200, entries.Count - 200);
        Save(entries);
    }

    public void ExportTo(string path, List<HistoryEntry> entries)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(entries, _opts));
    }

    public List<HistoryEntry> ImportFrom(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<HistoryEntry>>(json, _opts) ?? new List<HistoryEntry>();
    }
}

// Custom converter for int[,] since System.Text.Json doesn't handle 2D arrays natively
public class Int2DArrayConverter : JsonConverter<int[,]>
{
    public override int[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jagged = JsonSerializer.Deserialize<int[][]>(ref reader, options);
        if (jagged == null || jagged.Length == 0) return new int[0, 0];
        int rows = jagged.Length;
        int cols = jagged[0].Length;
        var result = new int[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                result[i, j] = jagged[i][j];
        return result;
    }

    public override void Write(Utf8JsonWriter writer, int[,] value, JsonSerializerOptions options)
    {
        int rows = value.GetLength(0);
        int cols = value.GetLength(1);
        writer.WriteStartArray();
        for (int i = 0; i < rows; i++)
        {
            writer.WriteStartArray();
            for (int j = 0; j < cols; j++)
                writer.WriteNumberValue(value[i, j]);
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }
}
