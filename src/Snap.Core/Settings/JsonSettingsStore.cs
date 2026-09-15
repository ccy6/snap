using System.Text.Json;

namespace Snap.Core.Settings;

public sealed class JsonSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;

    public JsonSettingsStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = Path.GetFullPath(filePath);
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return AppSettings.CreateDefault();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? AppSettings.CreateDefault();
        }
        catch (JsonException)
        {
            return AppSettings.CreateDefault();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directoryPath = Path.GetDirectoryName(_filePath)
            ?? throw new InvalidOperationException("The settings path has no parent directory.");
        Directory.CreateDirectory(directoryPath);

        var temporaryPath = Path.Join(directoryPath, Path.GetRandomFileName());
        try
        {
            var json = JsonSerializer.Serialize(settings, SerializerOptions);
            await File.WriteAllTextAsync(temporaryPath, json, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
