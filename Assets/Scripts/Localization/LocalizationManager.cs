using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KeepCoreSafe.Localization
{
    public static class LocalizationManager
    {
        private const string ResourcePath = "i18n";
        private const string PrefsKey = "KeepCoreSafe.Locale";
        private const string DefaultLocale = "en";

        private static readonly Dictionary<string, LanguageTable> tables = new();
        private static readonly List<LocalizationLanguageInfo> availableLanguages = new();
        private static readonly HashSet<string> reportedFormatFailures = new();
        private static bool isInitialized;
        private static string currentLocale = DefaultLocale;

        public static event Action LanguageChanged;

        public static IReadOnlyList<LocalizationLanguageInfo> AvailableLanguages
        {
            get
            {
                EnsureInitialized();
                return availableLanguages;
            }
        }

        public static string CurrentLocale
        {
            get
            {
                EnsureInitialized();
                return currentLocale;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            Initialize();
        }

        public static void Initialize()
        {
            tables.Clear();
            availableLanguages.Clear();

            foreach (TextAsset asset in Resources.LoadAll<TextAsset>(ResourcePath))
            {
                if (asset == null
                    || !LocalizationJsonParser.TryReadMeta(
                        asset.text,
                        out string locale,
                        out string displayName))
                {
                    Debug.LogWarning($"Invalid localization JSON: {asset?.name ?? "null"}");
                    continue;
                }

                Dictionary<string, string> strings =
                    LocalizationJsonParser.ParseFlatStringTable(asset.text);
                tables[locale] = new LanguageTable(locale, displayName, strings);
                availableLanguages.Add(new LocalizationLanguageInfo(locale, displayName));
            }

            availableLanguages.Sort(CompareLanguages);
            currentLocale = ResolveInitialLocale();
            isInitialized = true;
        }

        public static bool ChangeLanguage(string locale)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(locale)
                || !tables.ContainsKey(locale)
                || currentLocale == locale)
            {
                return false;
            }

            currentLocale = locale;
            PlayerPrefs.SetString(PrefsKey, currentLocale);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
            return true;
        }

        public static string Get(string key)
        {
            return Get(key, key);
        }

        public static string Get(string key, string fallback)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(key))
                return fallback ?? string.Empty;

            if (TryGetFrom(currentLocale, key, out string value))
                return value;

            if (currentLocale != DefaultLocale
                && TryGetFrom(DefaultLocale, key, out value))
            {
                return value;
            }

            foreach (LocalizationLanguageInfo language in availableLanguages)
            {
                if (TryGetFrom(language.Locale, key, out value))
                    return value;
            }

            return fallback ?? key;
        }

        public static string Format(string key, params object[] args)
        {
            return FormatResolvedString(key, Get(key), args);
        }

        public static string Format(string key, string fallback, params object[] args)
        {
            return FormatResolvedString(key, Get(key, fallback), args);
        }

        public static bool HasKey(string key)
        {
            EnsureInitialized();
            return !string.IsNullOrWhiteSpace(key)
                   && tables.TryGetValue(currentLocale, out LanguageTable table)
                   && table.Strings.ContainsKey(key);
        }

        private static void EnsureInitialized()
        {
            if (!isInitialized)
                Initialize();
        }

        private static bool TryGetFrom(string locale, string key, out string value)
        {
            value = null;
            return tables.TryGetValue(locale, out LanguageTable table)
                   && table.Strings.TryGetValue(key, out value)
                   && !string.IsNullOrEmpty(value);
        }

        private static string FormatResolvedString(
            string key,
            string format,
            object[] args)
        {
            format ??= string.Empty;
            args ??= Array.Empty<object>();

            try
            {
                return string.Format(format, args);
            }
            catch (FormatException exception)
            {
                LogFormatFailure(key, format, args, exception);
                return format;
            }
        }

        private static void LogFormatFailure(
            string key,
            string format,
            object[] args,
            FormatException exception)
        {
            string warningId = $"{key}|{format}|{args.Length}";
            if (!reportedFormatFailures.Add(warningId))
                return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(
                "[Localization] Format failed.\n"
                + $"Key: {key}\n"
                + $"Format: \"{format}\"\n"
                + $"Args Count: {args.Length}\n"
                + $"Args: {BuildArgumentsLog(args)}\n"
                + $"Reason: {exception.Message}");
#else
            Debug.LogWarning($"[Localization] Format failed. Key: {key}");
#endif
        }

        private static string BuildArgumentsLog(object[] args)
        {
            if (args.Length == 0)
                return "(none)";

            StringBuilder builder = new();
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append('[');
                builder.Append(i);
                builder.Append("]=");
                builder.Append(args[i] ?? "null");
            }

            return builder.ToString();
        }

        private static string ResolveInitialLocale()
        {
            string saved = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(saved) && tables.ContainsKey(saved))
                return saved;

            if (tables.ContainsKey(DefaultLocale))
                return DefaultLocale;

            return availableLanguages.Count > 0
                ? availableLanguages[0].Locale
                : DefaultLocale;
        }

        private static int CompareLanguages(
            LocalizationLanguageInfo a,
            LocalizationLanguageInfo b)
        {
            if (a.Locale == DefaultLocale && b.Locale != DefaultLocale)
                return -1;
            if (b.Locale == DefaultLocale && a.Locale != DefaultLocale)
                return 1;
            return string.CompareOrdinal(a.Locale, b.Locale);
        }

        private sealed class LanguageTable
        {
            public LanguageTable(
                string locale,
                string displayName,
                Dictionary<string, string> strings)
            {
                Locale = locale;
                DisplayName = displayName;
                Strings = strings;
            }

            public string Locale { get; }
            public string DisplayName { get; }
            public Dictionary<string, string> Strings { get; }
        }
    }
}
