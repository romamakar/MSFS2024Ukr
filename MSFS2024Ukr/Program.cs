using Google.Cloud.Translation.V2;
using Newtonsoft.Json;
using System.CodeDom;

namespace MSFS2024Ukr
{
    internal class Program
    {
        static AppStateStorage appStateStorage = new AppStateStorage(Path.Combine(AppContext.BaseDirectory, "state", "app.json"));
        static AppState appState = appStateStorage.LoadAsync().Result;
        public static int allSymbols = 0;
        static string apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        // Створюємо клієнт для Google Translation API
        static TranslationClient client = null;
        static void Main(string[] args)
        {
            appState.LastDate = GetUkrainianTime();
            appStateStorage.SaveAsync(appState).Wait();
            Console.WriteLine($"apiKey: {apiKey}");
            Console.WriteLine($"API key configured: {!string.IsNullOrWhiteSpace(apiKey)}");
            Console.WriteLine($"API key length: {apiKey?.Length ?? 0}");
            Console.WriteLine($"BaseDirectory: {AppContext.BaseDirectory}");
            ContnueWork();
            //oldCode();
            //allzise("C:\\Users\\roman\\OneDrive\\Desktop\\flight\\MSFS2024Ukr\\MSFS2024Ukr\\data2");
            //LocPakFile.SyncFromNewestDataAndTranslate("C:\\Users\\roman\\OneDrive\\Desktop\\flight\\MSFS2024Ukr\\MSFS2024Ukr\\data2", "C:\\Users\\roman\\OneDrive\\Desktop\\flight\\MSFS2024Ukr\\MSFS2024Ukr\\data3");
        }

        private static void ContnueWork()
        {
            if (appState.LastDate.Month != GetUkrainianTime().Month)
            {
                appState.CurrentSymbols = 0;
            }

            if (appState.CurrentSymbols >= 500000 && appState.LastDate.Month == GetUkrainianTime().Month)
            {
                Console.WriteLine($"CurrentSymbols: {appState.CurrentSymbols}");
                Console.WriteLine($"LastFilePath: {appState.LastFilePath}");
                Console.WriteLine($"LastDate: {appState.LastDate}");
                Console.WriteLine($"LastKey: {appState.LastKey}");
            }

            var result = LocPakFile.SyncFromNewestDataAndTranslate(Path.Combine(AppContext.BaseDirectory, "newestdata"), Path.Combine(AppContext.BaseDirectory, "data"));
            appStateStorage.SaveAsync(appState).Wait();
            Console.WriteLine($"Result of translation: {JsonConvert.SerializeObject(result)}");
            if (result.WasError)
            {
                throw new Exception("Сталася помилка під час синхронізації та перекладу файлів .locPak. Будь ласка, перевірте журнали для отримання додаткової інформації.");
            }

            LocPakFile.MakeArftifactDirectory(Path.Combine(AppContext.BaseDirectory, "data"), Path.Combine(AppContext.BaseDirectory, "MSFS2024ukr-en"), "en-US");
            LocPakFile.MakeArftifactDirectory(Path.Combine(AppContext.BaseDirectory, "data"), Path.Combine(AppContext.BaseDirectory, "MSFS2024ukr-pl"), "pl-PL");
            LocPakFile.MakeArftifactDirectory(Path.Combine(AppContext.BaseDirectory, "data"), Path.Combine(AppContext.BaseDirectory, "MSFS2024ukr-ru"), "ru-RU");
            LayoutFile.UpdateLocPakSizes(Path.Combine(AppContext.BaseDirectory, "MSFS2024ukr-en"));
            LayoutFile.UpdateLocPakSizes(Path.Combine(AppContext.BaseDirectory, "MSFS2024ukr-pl"));
            LayoutFile.UpdateLocPakSizes(Path.Combine(AppContext.BaseDirectory, "MSFS2024ukr-ru"));
        }

        public static string TranslateText(string text, string file, string key)
        {
            if (string.IsNullOrWhiteSpace(text.Trim()) || !containsCyrillic(text))
            {
                return text;
            }
            appState.CurrentSymbols += text.Length;
            
            if (appState.CurrentSymbols > 500000)
            {
                appStateStorage.SaveAsync(new AppState { CurrentSymbols = appState.CurrentSymbols, LastFilePath = file, LastDate = GetUkrainianTime(), LastKey = key }).Wait();
                throw new Exception("Перевищено ліміт символів для перекладу.");
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
                appStateStorage.SaveAsync(new AppState { CurrentSymbols = appState.CurrentSymbols, LastFilePath = file, LastDate = GetUkrainianTime(), LastKey = key }).Wait();
                throw;
            }
        }

        public static DateTime GetUkrainianTime()
        {
            var timeZoneId = OperatingSystem.IsWindows()
                ? "FLE Standard Time"
                : "Europe/Kyiv";

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                timeZone);
        }

        static void oldCode()
        {
            var filePath = Path.Combine(
              AppContext.BaseDirectory,
              "ukr",
              "pl-PL.locPak");

            var locPak = LocPakFile.Read(filePath);
            var totalLength = locPak.LocalisationPackage.Strings.Values.Sum(x => x.Length);
            var sum = 0;
            var lastKey = "";
            var found = false;
            foreach (var item in locPak.LocalisationPackage.Strings)
            {
                if (item.Key == "INPUT.KEY_DEVMODE_REDO")
                {
                    found = true;
                    continue;
                }

                if (found)
                {
                    if (string.IsNullOrEmpty(item.Value?.Trim()) || !containsCyrillic(item.Value))
                    {
                        continue;
                    }
                    sum += item.Value.Length;

                    lastKey = item.Key;
                    Console.WriteLine($"Total length of all strings: {sum}");

                    locPak.LocalisationPackage.Strings[item.Key] = TranslateText(item.Value, filePath, item.Key);
                }
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
            allSymbols = sum;
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
