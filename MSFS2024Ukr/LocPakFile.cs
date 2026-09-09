using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MSFS2024Ukr
{
    public sealed class LocPakFile
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public LocalisationPackage LocalisationPackage { get; set; } = new();

        public static LocPakFile Read(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<LocPakFile>(json, SerializerOptions)
                ?? throw new InvalidDataException($"Не вдалося прочитати файл '{path}'.");
        }

        public static async Task<LocPakFile> ReadAsync(string path, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<LocPakFile>(stream, SerializerOptions, cancellationToken)
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

        public static LocPakSyncResult SyncFromNewestDataAndTranslate(string newestDataRoot, string dataRoot)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(newestDataRoot);
            ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);

            var result = new LocPakSyncResult();
            var sourceLocPakFiles = EnumerateLocPakFiles(newestDataRoot);
            var targetLocPakFiles = EnumerateLocPakFiles(dataRoot)
                .ToDictionary(path => Path.GetRelativePath(dataRoot, path), StringComparer.OrdinalIgnoreCase);
            var newFiles = new List<string>();
            foreach (var sourceLocPakPath in sourceLocPakFiles)
            {
                var relativePath = Path.GetRelativePath(newestDataRoot, sourceLocPakPath);

                if (!targetLocPakFiles.TryGetValue(relativePath, out var targetLocPakPath))
                {
                    targetLocPakPath = Path.Combine(dataRoot, relativePath);

                    var targetDirectoryPath = Path.GetDirectoryName(targetLocPakPath);
                    if (!string.IsNullOrEmpty(targetDirectoryPath))
                    {
                        Directory.CreateDirectory(targetDirectoryPath);
                    }

                    File.Copy(sourceLocPakPath, targetLocPakPath);
                    targetLocPakFiles[relativePath] = targetLocPakPath;
                    result.CopiedFiles++;
                    newFiles.Add(targetLocPakPath);
                    continue;
                }

                var sourceLocPak = Read(sourceLocPakPath);
                var targetLocPak = Read(targetLocPakPath);
                var fileChanged = false;

                foreach (var sourceString in sourceLocPak.LocalisationPackage.Strings)
                {
                    if (targetLocPak.LocalisationPackage.Strings.ContainsKey(sourceString.Key))
                    {
                        continue;
                    }

                    targetLocPak.LocalisationPackage.Strings[sourceString.Key] = sourceString.Value; //Program.TranslateText(sourceString.Value);
                    result.AddedStrings++;
                    fileChanged = true;
                }

                if (!fileChanged)
                {
                    continue;
                }

                targetLocPak.Write(targetLocPakPath);
                result.UpdatedFiles++;
            }

            foreach (var newFile in newFiles)
            {
                var locPak = Read(newFile);
                foreach (var key in locPak.LocalisationPackage.Strings.Keys.ToList())
                {
                    locPak.LocalisationPackage.Strings[key] = Program.TranslateText(locPak.LocalisationPackage.Strings[key]);
                }
                locPak.Write(newFile);
            }

            return result;
        }

        private static IEnumerable<string> EnumerateLocPakFiles(string rootFolder)
        {
            return Directory
                .EnumerateFiles(rootFolder, "*", SearchOption.AllDirectories)
                .Where(path => string.Equals(Path.GetExtension(path), ".locPak", StringComparison.OrdinalIgnoreCase));
        }
    }

    public sealed class LocPakSyncResult
    {
        public int CopiedFiles { get; set; }

        public int UpdatedFiles { get; set; }

        public int AddedStrings { get; set; }
    }

    public sealed class LocalisationPackage
    {
        public string Language { get; set; } = string.Empty;

        public Dictionary<string, string> Strings { get; set; } = new(StringComparer.Ordinal);

        public string? GetString(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return Strings.GetValueOrDefault(key);
        }

        public void SetString(string key, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            Strings[key] = value;
        }
    }
}
