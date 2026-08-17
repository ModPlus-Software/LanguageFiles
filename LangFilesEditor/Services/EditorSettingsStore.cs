namespace LangFilesEditor.Services;

using System.IO;
using System.Text.Json;
using Models;

/// <summary>
/// Загрузка и сохранение <see cref="EditorSettings"/> в профиле пользователя.
/// </summary>
public sealed class EditorSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Загружает настройки с диска или возвращает значения по умолчанию.
    /// </summary>
    public static EditorSettingsStore Load()
    {
        var store = new EditorSettingsStore();
        try
        {
            var path = GetSettingsFilePath();
            if (!File.Exists(path))
            {
                Instance = store;
                return store;
            }

            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<EditorSettings>(json, JsonOptions);
            if (loaded != null)
            {
                store.Current = loaded;
            }
        }
        catch
        {
            store.Current = new EditorSettings();
        }

        Instance = store;
        return store;
    }

    /// <summary>
    /// Текущий глобальный экземпляр настроек.
    /// </summary>
    public static EditorSettingsStore Instance { get; private set; } = new();

    /// <summary>
    /// Текущий снимок настроек.
    /// </summary>
    public EditorSettings Current { get; private set; } = new();

    /// <summary>
    /// Сохраняет <see cref="Current"/> на диск.
    /// </summary>
    public void Save()
    {
        try
        {
            var path = GetSettingsFilePath();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(Current, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch
        {
            // настройки не должны ронять приложение
        }
    }

    private static string GetSettingsFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "LangFilesEditor", "settings.json");
    }
}