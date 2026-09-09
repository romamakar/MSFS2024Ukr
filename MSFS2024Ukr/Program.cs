using Google.Cloud.Translation.V2;
using System.CodeDom;

namespace MSFS2024Ukr
{
    internal class Program
    {
        static AppStateStorage appStateStorage = new AppStateStorage(Path.Combine(AppContext.BaseDirectory, "state", "app.json"));
        static AppState appState = appStateStorage.LoadAsync().Result;
        static string apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        // Створюємо клієнт для Google Translation API
        static TranslationClient client = null;
        static void Main(string[] args)
        {
            appState.LastDate = DateTime.Now;
            appStateStorage.SaveAsync(appState).Wait();
            Console.WriteLine($"apiKey: {apiKey}");
            Console.WriteLine($"BaseDirectory: {AppContext.BaseDirectory}");

            var files = Directory
                  .EnumerateFiles(AppContext.BaseDirectory, "*", SearchOption.AllDirectories)
                  .Where(path => string.Equals(Path.GetExtension(path), ".*", StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                Console.WriteLine(file);
            }
            // allzise("C:\\Users\\roman\\OneDrive\\Desktop\\flight\\MSFS2024Ukr\\MSFS2024Ukr\\data");
        }

        public static string TranslateText(string text, string file)
        {
            if (string.IsNullOrEmpty(text.Trim()) || !containsCyrillic(text))
            {
                return text;
            }
            appState.CurrentSymbols += text.Length;

            if (appState.CurrentSymbols > 500000)
            {
                appStateStorage.SaveAsync(new AppState { CurrentSymbols = appState.CurrentSymbols, LastFilePath = file, LastDate = DateTime.Now }).Wait();
                throw new Exception();
            }

            try
            {
                if (client == null)
                {
                    client = TranslationClient.CreateFromApiKey(apiKey);
                }
                string val = client.TranslateText(text, "uk", sourceLanguage: "ru", model: TranslationModel.ServiceDefault).TranslatedText;
                val = val.Replace("«", "\"").Replace("»", "\"");
                return val;
            }
            catch (Exception)
            {
                appStateStorage.SaveAsync(new AppState { CurrentSymbols = appState.CurrentSymbols, LastFilePath = file, LastDate = DateTime.Now }).Wait();
                throw;
            }
        }

        static void oldCode()
        {
            var filePath = Path.Combine(
              AppContext.BaseDirectory,
              "data",
              "Content",
              "Packages",
              "fs-base",
              "ru-RU.locPak");

            var locPak = LocPakFile.Read(filePath);
            var totalLength = locPak.LocalisationPackage.Strings.Values.Sum(x => x.Length);
            var sum = 0;
            var lastKey = "";
            foreach (var item in locPak.LocalisationPackage.Strings)
            {
                if (string.IsNullOrEmpty(item.Value?.Trim()) || !containsCyrillic(item.Value))
                {
                    continue;
                }
                sum += item.Value.Length;
                if (sum > 500000)
                {
                    break;
                }
                lastKey = item.Key;
                Console.WriteLine($"Total length of all strings: {sum}");
                locPak.LocalisationPackage.Strings[item.Key] = TranslateText(item.Value, filePath);
            }

            Console.WriteLine($"lastKey: {lastKey}");

            locPak.Write(filePath + "ukr");

            //lastKey: INPUT.KEY_DEVMODE_REDO
            //оновити layout.json size
        }

        static void allzise(string rootFolder)
        {
            var sum = 0;
            foreach (var filePath in Directory.EnumerateFiles(rootFolder, "*", SearchOption.AllDirectories)
                .Where(path => string.Equals(Path.GetExtension(path), ".locPak", StringComparison.OrdinalIgnoreCase)))
            {
                var locPak = LocPakFile.Read(filePath);
                foreach (var item in locPak.LocalisationPackage.Strings)
                {
                    if (string.IsNullOrEmpty(item.Value?.Trim()) || !containsCyrillic(item.Value))
                    {
                        continue;
                    }
                    sum += item.Value.Length;
                }
            }

            Console.WriteLine(sum);

        }



        static bool containsCyrillic(string text)
        {
            foreach (char c in text)
            {
                if ((c >= '\u0400' && c <= '\u04FF') || // Cyrillic
                    (c >= '\u0500' && c <= '\u052F') || // Cyrillic Supplement
                    (c >= '\u2DE0' && c <= '\u2DFF') || // Cyrillic Extended-A
                    (c >= '\uA640' && c <= '\uA69F'))   // Cyrillic Extended-B
                {
                    return true;
                }
            }
            return false;
        }
    }
}
