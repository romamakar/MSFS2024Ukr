using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MSFS2024Ukr
{
    public sealed class LayoutFile
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        [JsonPropertyName("content")]
        public List<LayoutEntry> Content { get; set; } = [];

        public static LayoutFile Read(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<LayoutFile>(json, SerializerOptions)
                ?? throw new InvalidDataException($"Не вдалося прочитати файл '{path}'.");
        }

        public static ManifestFile ReadManifest(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<ManifestFile>(json, SerializerOptions)
                ?? throw new InvalidDataException($"Не вдалося прочитати файл '{path}'.");
        }

        public static async Task<LayoutFile> ReadAsync(string path, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<LayoutFile>(stream, SerializerOptions, cancellationToken)
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

        public LayoutEntry? FindByPath(string relativePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
            return Content.FirstOrDefault(x => string.Equals(x.Path, relativePath, StringComparison.OrdinalIgnoreCase));
        }

        public static int UpdateLocPakSizes(string rootFolder)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootFolder);

            var updatedEntriesCount = 0;

            foreach (var locPakPath in Directory.EnumerateFiles(rootFolder, "*", SearchOption.AllDirectories))
            {
                if (!string.Equals(Path.GetExtension(locPakPath), ".locPak", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var directoryPath = Path.GetDirectoryName(locPakPath);
                if (string.IsNullOrEmpty(directoryPath))
                {
                    continue;
                }

                var layoutPath = Path.Combine(directoryPath, "layout.json");
                if (!File.Exists(layoutPath))
                {
                    continue;
                }

                var layout = Read(layoutPath);
                var fileName = Path.GetFileName(locPakPath);
                var fileSize = new FileInfo(locPakPath).Length;
                var entry = layout.FindByPath(fileName);

                if (entry is null || entry.Size == fileSize)
                {
                    continue;
                }

                entry.Size = fileSize;
                layout.Write(layoutPath);
                updatedEntriesCount++;
            }

            return updatedEntriesCount;
        }

        public static void sum(string rootFolder)
        {

            foreach (var path in Directory.EnumerateFiles(rootFolder, "layout.json", SearchOption.AllDirectories))
            {
                long size = 0;

                var layout = Read(path);

                foreach (var s in layout.Content)
                {
                    size += s?.Size ?? 0;
                }

                var directoryPath = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directoryPath))
                {
                    continue;
                }

                var layoutPath = Path.Combine(directoryPath, "manifest.json");
                var manfest = ReadManifest(layoutPath);
            }
        }
    }





        public sealed class LayoutEntry
        {
            [JsonPropertyName("path")]
            public string Path { get; set; } = string.Empty;

            [JsonPropertyName("size")]
            public long Size { get; set; }

            [JsonPropertyName("date")]
            public long Date { get; set; }
        }

    public sealed class ManifestFile
    {
        [JsonPropertyName("dependencies")]
        public string dependencies { get; set; } = string.Empty;

        [JsonPropertyName("content_type")]
        public long content_type { get; set; }

        [JsonPropertyName("title")]
        public long title { get; set; }
        [JsonPropertyName("manufacturer")]
        public string manufacturer { get; set; } = string.Empty;

        [JsonPropertyName("creator")]
        public long creator { get; set; }

        [JsonPropertyName("date")]
        public long Date { get; set; }
        [JsonPropertyName("dependencies")]
        public string dependencies { get; set; } = string.Empty;

        [JsonPropertyName("content_type")]
        public long content_type { get; set; }

        [JsonPropertyName("date")]
        public long Date { get; set; }
    }
}
