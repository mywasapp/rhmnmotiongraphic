using System.Text.Json;

namespace Rhmn.Core;

public static class ProjectFiles
{
    public static string ValidateEntry(string input)
    {
        var file = Path.GetFullPath(input);
        if (!File.Exists(file)) throw new FileNotFoundException("File HTML tidak ditemukan.", file);
        if (Path.GetExtension(file).ToLowerInvariant() is not (".html" or ".htm")) throw new ArgumentException("Pilih file HTML utama yang memuat CSS, JavaScript, dan aset proyek.");
        return file;
    }
    public static string[] FindEntries(string folder)
    {
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException("Folder proyek tidak ditemukan.");
        var files = Directory.EnumerateFiles(folder).Where(f => Path.GetExtension(f).ToLowerInvariant() is ".html" or ".htm").OrderBy(f => f).ToArray();
        var index = files.FirstOrDefault(f => Path.GetFileName(f).Equals("index.html",StringComparison.OrdinalIgnoreCase));
        return index is null ? files : [index];
    }
    public static string SafeName(string name)
    {
        const string invalid = "<>:\"/\\|?*";
        var value = new string(name.Select(c => char.IsControl(c) || invalid.Contains(c) ? '_' : c).ToArray()).Trim(' ','.');
        if (string.IsNullOrEmpty(value)) value = "motion";
        var stem = value.Split('.')[0].ToUpperInvariant();
        if (new[] {"CON","PRN","AUX","NUL","COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9","LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"}.Contains(stem)) value = "_" + value;
        return value.Length > 100 ? value[..100] : value;
    }
    public static string UniqueOutput(string folder, string entry, IEnumerable<string> reserved)
    {
        var used = reserved.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var name = SafeName(Path.GetFileNameWithoutExtension(entry));
        if (name.Equals("index",StringComparison.OrdinalIgnoreCase)) name = SafeName(new DirectoryInfo(Path.GetDirectoryName(entry)!).Name);
        var path = Path.Combine(folder, name + ".mp4");
        for (var n=2; File.Exists(path) || used.Contains(path); n++) path = Path.Combine(folder, $"{name}-{n}.mp4");
        return path;
    }
}
public static class JsonFiles
{
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public static void Save<T>(string path, T value)
    {
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var temp = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temp,JsonSerializer.Serialize(value,Options)); File.Move(temp,full,true); }
        finally { if(File.Exists(temp)) File.Delete(temp); }
    }
    public static T? Load<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path),Options);
}
