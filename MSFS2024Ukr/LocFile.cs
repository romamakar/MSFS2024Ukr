using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MSFS2024Ukr
{
    public sealed class LocFile
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public LocalisationFile LocalisationFile { get; set; } = new();

        public static LocFile Read(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<LocFile>(json, SerializerOptions)
                ?? throw new InvalidDataException($"Не вдалося прочитати файл '{path}'.");
        }

        public static async Task<LocFile> ReadAsync(string path, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<LocFile>(stream, SerializerOptions, cancellationToken)
                ?? throw new InvalidDataException($"Не вдалося прочитати файл '{path}'.");
        }

        public void Write(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            var directoryPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var json = JsonSerializer.Serialize(this, SerializerOptions);
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }

        public async Task WriteAsync(string path, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            var directoryPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, this, SerializerOptions, cancellationToken);
        }
    }

    public sealed class LocalisationFile
    {
        public int Version { get; set; }

        public string UUID { get; set; } = string.Empty;

        public List<string> Languages { get; set; } = new();

        public Dictionary<string, LocalisationStringEntry> Strings { get; set; } = new(StringComparer.Ordinal);

        public LocalisationStringEntry? GetString(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return Strings.GetValueOrDefault(key);
        }
    }

    public sealed class LocalisationStringEntry
    {
        public string Name { get; set; } = string.Empty;

        public string LastModifiedBy { get; set; } = string.Empty;

        public string LastModifiedDate { get; set; } = string.Empty;

        public string LocalizationStatus { get; set; } = string.Empty;

        public Dictionary<string, LocalisedLanguageText> Languages { get; set; } = new(StringComparer.Ordinal);

        public string? GetText(string language)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(language);
            return Languages.GetValueOrDefault(language)?.Text;
        }

        public void SetText(string language, string text, string? localizationStatus = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(language);

            if (!Languages.TryGetValue(language, out var entry))
            {
                entry = new LocalisedLanguageText();
                Languages[language] = entry;
            }

            entry.Text = text;
            if (!string.IsNullOrWhiteSpace(localizationStatus))
            {
                entry.LocalizationStatus = localizationStatus;
            }
        }
    }

    public sealed class LocalisedLanguageText
    {
        public string Text { get; set; } = string.Empty;

        public string LocalizationStatus { get; set; } = string.Empty;
    }
}
