using LoneEftDmaRadar.Misc.JSON;
using System.Reflection;

namespace LoneEftDmaRadar.UI.Localization
{
    internal static class Loc
    {
        private static readonly Lock _lock = new();
        private static Dictionary<string, string> _translations = new(StringComparer.Ordinal);
        private static Dictionary<string, Dictionary<string, string>> _exitTranslations = new(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized;

        private static string _language = "zh-CN";

        public static string CurrentLanguage
        {
            get
            {
                lock (_lock)
                {
                    return _language;
                }
            }
        }

        public static void SetLanguage(string language)
        {
            string normalized = NormalizeLanguage(language);

            bool shouldReload;
            lock (_lock)
            {
                shouldReload = !string.Equals(_language, normalized, StringComparison.OrdinalIgnoreCase);
                _language = normalized;
            }

            if (_initialized && shouldReload)
                Reload();
        }

        public static FileInfo TranslationFile => new(
            Path.Combine(Program.ConfigPath.FullName, "lang", $"{CurrentLanguage}.json"));

        public static FileInfo ExitTranslationFile => new(
            Path.Combine(Program.ConfigPath.FullName, $"exits.{CurrentLanguage}.json"));

        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized)
                    return;
                _initialized = true;
            }

            Reload();
        }

        public static void Reload()
        {
            try
            {
                if (string.Equals(CurrentLanguage, "en", StringComparison.OrdinalIgnoreCase))
                {
                    lock (_lock)
                    {
                        _translations = new Dictionary<string, string>(StringComparer.Ordinal);
                        _exitTranslations = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                    }
                    return;
                }

                // Load Main Translations
                var file = TranslationFile;
                if (!file.Directory!.Exists)
                    file.Directory.Create();

                if (!file.Exists)
                {
                    // On first run (new machine), seed a usable default Chinese translation file
                    // from the embedded resource.
                    if (string.Equals(CurrentLanguage, "zh-CN", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!TryWriteEmbeddedDefault(file.FullName))
                            File.WriteAllText(file.FullName, "{}\n");
                    }
                    else
                    {
                        File.WriteAllText(file.FullName, "{}\n");
                    }
                }

                string json = File.ReadAllText(file.FullName);
                var loaded = JsonSerializer.Deserialize(json, AppJsonContext.Default.DictionaryStringString) ??
                             new Dictionary<string, string>();

                if (string.Equals(CurrentLanguage, "zh-CN", StringComparison.OrdinalIgnoreCase) &&
                    TryLoadEmbeddedDefaultTranslations(out var embeddedDefaults) &&
                    embeddedDefaults.Count > 0)
                {
                    bool changed = false;
                    foreach (var kvp in embeddedDefaults)
                    {
                        if (!loaded.TryGetValue(kvp.Key, out var existing) || string.IsNullOrWhiteSpace(existing))
                        {
                            if (!string.IsNullOrWhiteSpace(kvp.Value))
                            {
                                loaded[kvp.Key] = kvp.Value;
                                changed = true;
                            }
                        }
                    }

                    if (changed)
                    {
                        string mergedJson = JsonSerializer.Serialize(
                            loaded,
                            AppJsonContext.Default.DictionaryStringString);
                        File.WriteAllText(file.FullName, mergedJson);
                    }
                }

                // Load Exit Translations
                var exitFile = ExitTranslationFile;
                var exitTranslations = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

                // 1. Try to load from embedded resource first (if zh-CN)
                if (string.Equals(CurrentLanguage, "zh-CN", StringComparison.OrdinalIgnoreCase))
                {
                    TryLoadEmbeddedExitTranslations(out exitTranslations);
                }

                // 2. Try to load from disk (overrides embedded)
                if (exitFile.Exists)
                {
                    try
                    {
                        string exitJson = File.ReadAllText(exitFile.FullName);
                        var loadedExits = JsonSerializer.Deserialize(
                            exitJson,
                            AppJsonContext.Default.DictionaryStringDictionaryStringString);
                        
                        if (loadedExits is not null)
                        {
                            // Merge loaded exits into exitTranslations
                            foreach (var map in loadedExits)
                            {
                                if (!exitTranslations.TryGetValue(map.Key, out var mapExits))
                                {
                                    mapExits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                    exitTranslations[map.Key] = mapExits;
                                }

                                foreach (var exit in map.Value)
                                {
                                    mapExits[exit.Key] = exit.Value;
                                }
                            }
                        }
                    }
                    catch { }
                }

                lock (_lock)
                {
                    _translations = new Dictionary<string, string>(loaded, StringComparer.Ordinal);
                    _exitTranslations = exitTranslations;
                }
            }
            catch
            {
                // If localization fails, fall back to English strings.
                lock (_lock)
                {
                    _translations = new Dictionary<string, string>(StringComparer.Ordinal);
                    _exitTranslations = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        private static bool TryWriteEmbeddedDefault(string destinationPath)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                const string resourceName = "LoneEftDmaRadar.Resources.lang.zh-CN.json";
                using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream is null)
                    return false;

                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(json))
                    return false;

                File.WriteAllText(destinationPath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryLoadEmbeddedDefaultTranslations(out Dictionary<string, string> translations)
        {
            translations = new Dictionary<string, string>();
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                const string resourceName = "LoneEftDmaRadar.Resources.lang.zh-CN.json";
                using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream is null)
                    return false;

                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(json))
                    return false;

                translations = JsonSerializer.Deserialize(
                                   json,
                                   AppJsonContext.Default.DictionaryStringString) ??
                               new Dictionary<string, string>();
                return true;
            }
            catch
            {
                translations = new Dictionary<string, string>();
                return false;
            }
        }

        private static bool TryLoadEmbeddedExitTranslations(out Dictionary<string, Dictionary<string, string>> translations)
        {
            translations = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                const string resourceName = "LoneEftDmaRadar.Resources.lang.exits.zh-CN.json";
                using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream is null)
                    return false;

                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(json))
                    return false;

                translations = JsonSerializer.Deserialize(
                                   json,
                                   AppJsonContext.Default.DictionaryStringDictionaryStringString) ??
                               new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return "zh-CN";

            language = language.Trim();
            if (language.Equals("english", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("en", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("en-us", StringComparison.OrdinalIgnoreCase))
            {
                return "en";
            }

            if (language.Equals("chinese", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("zh", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("zh-cn", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("zh-hans", StringComparison.OrdinalIgnoreCase))
            {
                return "zh-CN";
            }

            return language;
        }

        public static string T(string english)
        {
            if (string.IsNullOrEmpty(english))
                return english;

            Initialize();

            lock (_lock)
            {
                if (_translations.TryGetValue(english, out var translated) &&
                    !string.IsNullOrWhiteSpace(translated))
                {
                    return translated;
                }
            }

            return english;
        }

        /// <summary>
        /// Translates only the visible part of an ImGui label, preserving the ID suffix.
        /// Supports "label##id" and "label###id".
        /// </summary>
        public static string WithId(string label)
        {
            if (string.IsNullOrEmpty(label))
                return label;

            int triple = label.IndexOf("###", StringComparison.Ordinal);
            if (triple >= 0)
            {
                string visible = label[..triple];
                string id = label[triple..];
                return T(visible) + id;
            }

            int dbl = label.IndexOf("##", StringComparison.Ordinal);
            if (dbl >= 0)
            {
                string visible = label[..dbl];
                string id = label[dbl..];
                return T(visible) + id;
            }

            return T(label);
        }

        /// <summary>
        /// Creates a translated ImGui label that keeps a stable internal ID via the "###" suffix.
        /// Example: Title("Settings") => "设置###Settings".
        /// </summary>
        public static string Title(string id)
        {
            if (string.IsNullOrEmpty(id))
                return id;
            return $"{T(id)}###{id}";
        }

        /// <summary>
        /// Translates an exit name for a specific map.
        /// </summary>
        public static string Exit(string mapName, string exitName)
        {
            if (string.IsNullOrEmpty(mapName) || string.IsNullOrEmpty(exitName))
                return exitName;

            Initialize();

            lock (_lock)
            {
                if (_exitTranslations.TryGetValue(mapName, out var mapExits) &&
                    mapExits.TryGetValue(exitName, out var translated) &&
                    !string.IsNullOrWhiteSpace(translated))
                {
                    return translated;
                }
            }

            return exitName;
        }
    }
}
