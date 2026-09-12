using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MSFS2024Ukr
{
    public sealed class ManifestFile
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        [JsonPropertyName("dependencies")]
        public List<ManifestDependency> Dependencies { get; set; } = [];

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; } = string.Empty;

        [JsonPropertyName("creator")]
        public string Creator { get; set; } = string.Empty;

        [JsonPropertyName("package_version")]
        public string PackageVersion { get; set; } = string.Empty;

        [JsonPropertyName("minimum_game_version")]
        public string MinimumGameVersion { get; set; } = string.Empty;

        [JsonPropertyName("minimum_compatibility_version")]
        public string MinimumCompatibilityVersion { get; set; } = string.Empty;

        [JsonPropertyName("export_type")]
        public string ExportType { get; set; } = string.Empty;

        [JsonPropertyName("builder")]
        public string Builder { get; set; } = string.Empty;

        [JsonPropertyName("package_order_hint")]
        public string PackageOrderHint { get; set; } = string.Empty;

        [JsonPropertyName("release_notes")]
        public ManifestReleaseNotes ReleaseNotes { get; set; } = new();

        [JsonPropertyName("total_package_size")]
        public string TotalPackageSize { get; set; } = string.Empty;

        public static ManifestFile Read(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<ManifestFile>(json, SerializerOptions)
                ?? throw new InvalidDataException($"Не вдалося прочитати файл '{path}'.");
        }

        public static async Task<ManifestFile> ReadAsync(string path, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<ManifestFile>(stream, SerializerOptions, cancellationToken)
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

    public sealed class ManifestDependency
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("package_version")]
        public string PackageVersion { get; set; } = string.Empty;
    }

    public sealed class ManifestReleaseNotes
    {
        [JsonPropertyName("neutral")]
        public ManifestReleaseNote Neutral { get; set; } = new();
    }

    public sealed class ManifestReleaseNote
    {
        [JsonPropertyName("LastUpdate")]
        public string LastUpdate { get; set; } = string.Empty;

        [JsonPropertyName("OlderHistory")]
        public string OlderHistory { get; set; } = string.Empty;
    }
}
