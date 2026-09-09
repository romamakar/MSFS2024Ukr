using System.Text.Json;

namespace MSFS2024Ukr
{
    public class AppStateStorage
    {
        private readonly string _filePath;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public AppStateStorage(string filePath)
        {
            _filePath = filePath;
        }

        public async Task<AppState> LoadAsync()
        {
            if (!File.Exists(_filePath))
            {
                return new AppState();
            }

            await using var stream = File.OpenRead(_filePath);

            return await JsonSerializer.DeserializeAsync<AppState>(
                       stream,
                       JsonOptions)
                   ?? new AppState();
        }

        public async Task SaveAsync(AppState state)
        {
            var directory = Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(_filePath);

            await JsonSerializer.SerializeAsync(
                stream,
                state,
                JsonOptions);
        }
    }
}
