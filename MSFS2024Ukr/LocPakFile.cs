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
 
            var existingFiles = new List<FileWithKeys>();
            foreach (var sourceLocPakPath in sourceLocPakFiles)
            {
                
                var relativePath = Path.GetRelativePath(newestDataRoot, sourceLocPakPath);

                var fileWithKeys = new FileWithKeys
                {
                    FilePath = relativePath,
                    Keys = new List<string>()
                };

                if (!targetLocPakFiles.TryGetValue(relativePath, out var targetLocPakPath))
                {
                    targetLocPakPath = Path.Combine(dataRoot, relativePath);

                    var targetDirectoryPath = Path.GetDirectoryName(targetLocPakPath);
                    if (!string.IsNullOrEmpty(targetDirectoryPath))
                    {
                        Directory.CreateDirectory(targetDirectoryPath);
                    }

                    File.Copy(sourceLocPakPath, targetLocPakPath);
                    var locPak = Read(targetLocPakPath);

                    bool isError = false;
                    var translatedKeys = new List<string>();
                    try
                    {
                        foreach (var key in locPak.LocalisationPackage.Strings.Keys.ToList())
                        {
                            locPak.LocalisationPackage.Strings[key] = Program.TranslateText(locPak.LocalisationPackage.Strings[key], targetLocPakPath, key);
                            translatedKeys.Add(key);
                            fileWithKeys.Keys.Add(key);
                        }
                    }
                    catch (Exception ex)
                    {
                        isError = true;
                        result.WasError = true;
                        locPak.LocalisationPackage.Strings = locPak.LocalisationPackage.Strings
                            .Where(kvp => translatedKeys.Contains(kvp.Key))
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);
                        Console.WriteLine($"Помилка при перекладі файлу '{targetLocPakPath}': {ex.Message}");
                    }
                    locPak.Write(targetLocPakPath);

                    if (isError)
                    {
                        Console.WriteLine($"Файл '{targetLocPakPath}' не був перекладений через помилку.");
                        existingFiles.Add(fileWithKeys);
                        break;
                    }
                    targetLocPakFiles[relativePath] = targetLocPakPath;
                    existingFiles.Add(fileWithKeys);
                    result.CopiedFiles++;
                   
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

                    try
                    {
                        targetLocPak.LocalisationPackage.Strings[sourceString.Key] = Program.TranslateText(sourceString.Value, targetLocPakPath, sourceString.Key);

                    }
                    catch (Exception ex)
                    {
                        result.WasError = true;
                        Console.WriteLine($"Помилка при перекладі ключа '{sourceString.Key}' у файлі '{targetLocPakPath}': {ex.Message}");
                        break;
                    }
                    fileWithKeys.Keys.Add(sourceString.Key);
                    result.AddedStrings++;
                    fileChanged = true;
                    targetLocPak.Write(targetLocPakPath);
                }


                if (!fileChanged)
                {
                    continue;
                }
               
                result.UpdatedFiles++;
                existingFiles.Add(fileWithKeys);
            }

      
            result.ExistingFiles = existingFiles;

            return result;
        }

        private static List<string> EnumerateLocPakFiles(string rootFolder)
        {
            return Directory
                .EnumerateFiles(rootFolder, "*", SearchOption.AllDirectories)
                .Where(path => string.Equals(Path.GetExtension(path), ".locPak", StringComparison.OrdinalIgnoreCase)).OrderBy(path => path).ToList();
        }

        public static void MakeArftifactDirectory(string sourceDirectory, string destinationDirectory, string language)
        {
            Directory.CreateDirectory(destinationDirectory);

            foreach (var file in Directory.GetFiles(sourceDirectory))
            {
                var fileName = Path.GetFileName(file);

                if (string.Equals(
                        Path.GetExtension(file),
                        ".locPak",
                        StringComparison.OrdinalIgnoreCase))
                {
                    fileName = $"{language}.locPak";
                }

                var destinationFile = Path.Combine(destinationDirectory, fileName);

                File.Copy(file, destinationFile, overwrite: true);
            }

            foreach (var directory in Directory.GetDirectories(sourceDirectory))
            {
                var directoryName = Path.GetFileName(directory);

                var destinationSubDirectory =
                    Path.Combine(destinationDirectory, directoryName);

                MakeArftifactDirectory(directory, destinationSubDirectory, language);
            }
        }
    }

    public sealed class LocPakSyncResult
    {
        public int CopiedFiles { get; set; }

        public int UpdatedFiles { get; set; }

        public int AddedStrings { get; set; }
        public List<FileWithKeys> ExistingFiles { get; set; }
        public bool WasError { get; set; } = false;
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
