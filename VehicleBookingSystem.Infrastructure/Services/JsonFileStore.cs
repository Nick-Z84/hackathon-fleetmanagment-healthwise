using System.Text.Json;
using System.Text.Json.Serialization;

namespace VehicleBookingSystem.Infrastructure.Services;

/// <summary>
/// Thread-safe, encrypted JSON file store for a single entity type.
/// Each instance corresponds to one physical file.
/// </summary>
public sealed class JsonFileStore<T> where T : class
{
    private readonly string _filePath;
    private readonly DataEncryptor _encryptor;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<T>? _cache;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonFileStore(string dataDirectory, string fileName, DataEncryptor encryptor)
    {
        Directory.CreateDirectory(dataDirectory);
        _filePath  = Path.Combine(dataDirectory, fileName);
        _encryptor = encryptor;
    }

    /// <summary>
    /// Returns a snapshot copy of the cached list so callers cannot
    /// accidentally mutate the live cache while others are iterating it.
    /// </summary>
    public async Task<List<T>> LoadAsync()
    {
        if (_cache is not null) return new List<T>(_cache); // defensive copy

        await _lock.WaitAsync();
        try
        {
            if (_cache is not null) return new List<T>(_cache);

            if (!File.Exists(_filePath))
            {
                _cache = [];
                return [];
            }

            var bytes = await File.ReadAllBytesAsync(_filePath);
            var json  = _encryptor.Decrypt(bytes);
            _cache = JsonSerializer.Deserialize<List<T>>(json, JsonOpts) ?? [];
            return new List<T>(_cache);
        }
        finally { _lock.Release(); }
    }

    public async Task SaveAsync(IEnumerable<T> items)
    {
        await _lock.WaitAsync();
        try
        {
            var list = items.ToList();
            _cache   = list;
            var json      = JsonSerializer.Serialize(list, JsonOpts);
            var encrypted = _encryptor.Encrypt(json);
            await File.WriteAllBytesAsync(_filePath, encrypted);
        }
        finally { _lock.Release(); }
    }

    public void InvalidateCache() => _cache = null;
}
